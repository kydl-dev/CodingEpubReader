using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DesktopUI.ViewModels;
using ReactiveUI.Avalonia;
using Serilog;
using Shared.Exceptions;
using WebViewCore;
using static System.Reflection.BindingFlags;

namespace DesktopUI.Views;

public partial class EpubReaderView : ReactiveUserControl<EpubReaderViewModel>
{
    private readonly DispatcherTimer _progressPollTimer;
    private bool _isProgressPollInFlight;
    private bool _isViewLoaded;
    private int _lastAppliedSearchFocusRequestId;
    private string _pendingHtml = string.Empty;
    private int _renderVersion;
    private IWebViewControl? _resolvedTypedWebView;
    private Control? _resolvedWebView;
    private EpubReaderViewModel? _subscribedViewModel;
    private DateTime _suspendProgressSamplingUntilUtc = DateTime.MinValue;

    public EpubReaderView()
    {
        InitializeComponent();

        _progressPollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _progressPollTimer.Tick += OnProgressPollTimerTick;

        DataContextChanged += OnDataContextChanged;
        var tocTreeView = ResolveTocTreeView();
        tocTreeView?.AddHandler(PointerReleasedEvent, OnTocTreePointerReleased, RoutingStrategies.Bubble);
        if (tocTreeView != null) tocTreeView.LayoutUpdated += OnTocTreeLayoutUpdated;
        AttachedToVisualTree += (_, _) =>
        {
            _isViewLoaded = true;
            _progressPollTimer.Start();
            if (_subscribedViewModel != null) RenderHtml(_subscribedViewModel.CurrentChapterContent);
        };

        DetachedFromVisualTree += (_, _) =>
        {
            _progressPollTimer.Stop();
            _ = PersistReadingProgressAsync(true);

            if (_resolvedWebView is IDisposable disposableWebView)
                try
                {
                    disposableWebView.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to dispose WebView cleanly. Error: {Error}", ex.FullMessage());
                }

            _resolvedTypedWebView = null;
            _resolvedWebView = null;
        };
    }

    private void OnTocTreeLayoutUpdated(object? sender, EventArgs e)
    {
        var tocScrollViewer = ResolveTocScrollViewer();
        if (tocScrollViewer == null || tocScrollViewer.Offset.X <= 0) return;

        tocScrollViewer.Offset = new Vector(0, tocScrollViewer.Offset.Y);
    }

    private void OnTocTreePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var tocScrollViewer = ResolveTocScrollViewer();
        if (tocScrollViewer == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var resolved = ResolveTocScrollViewer();
            if (resolved == null) return;

            resolved.Offset = new Vector(0, resolved.Offset.Y);
        }, DispatcherPriority.Background);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel != null) _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _subscribedViewModel = DataContext as EpubReaderViewModel;
        if (_subscribedViewModel == null) return;

        _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _suspendProgressSamplingUntilUtc = DateTime.UtcNow.AddSeconds(1);
        RenderHtml(_subscribedViewModel.CurrentChapterContent, !_isViewLoaded);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not EpubReaderViewModel vm) return;

        if (e.PropertyName == nameof(EpubReaderViewModel.CurrentChapterContent))
        {
            _suspendProgressSamplingUntilUtc = DateTime.UtcNow.AddSeconds(1);
            RenderHtml(vm.CurrentChapterContent);
            _ = RestoreReadingProgressAsync(vm.CurrentChapterProgress);
            return;
        }

        if (e.PropertyName == nameof(EpubReaderViewModel.SearchFocusRequest))
        {
            _ = ApplySearchFocusAsync(vm.SearchFocusRequest);
            return;
        }

        if (e.PropertyName == nameof(EpubReaderViewModel.IsLoading) && !vm.IsLoading)
        {
            // The embedded WebView can still be initializing when the first chapter arrives.
            // Re-render once loading completes so the initial chapter/cover isn't lost.
            _suspendProgressSamplingUntilUtc = DateTime.UtcNow.AddSeconds(1);
            Dispatcher.UIThread.Post(() => RenderHtml(vm.CurrentChapterContent), DispatcherPriority.Background);
        }
    }

    private void RenderHtml(string html, bool forceQueue = false)
    {
        var content = html ?? string.Empty;
        _pendingHtml = WrapAsDocument(content);
        var renderVersion = ++_renderVersion;
        var webView = ResolveWebView();

        if (webView == null)
        {
            Log.Warning("RenderHtml skipped: ChapterWebView is null.");
            return;
        }

        if (forceQueue || !_isViewLoaded)
        {
            Log.Debug(
                "RenderHtml queued: forceQueue={ForceQueue}, isViewLoaded={IsViewLoaded}, htmlLength={HtmlLength}",
                forceQueue,
                _isViewLoaded,
                _pendingHtml.Length);
            return;
        }

        var webViewType = webView.GetType();
        Log.Information(
            "RenderHtml start: webViewType={WebViewType}, htmlLength={HtmlLength}",
            webViewType.FullName,
            _pendingHtml.Length);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            TryNavigateViaTempFile(webView, webViewType, _pendingHtml, renderVersion))
            return;

        if (webView is IWebViewControl typedWebView)
        {
            try
            {
                typedWebView.NavigateToString(_pendingHtml);
                _ = RestoreReadingProgressAsync(_subscribedViewModel?.CurrentChapterProgress ?? 0.0);
                Log.Information("RenderHtml path: IWebViewControl.NavigateToString");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RenderHtml failed in IWebViewControl.NavigateToString. Error: {Error}",
                    ex.FullMessage());
                throw;
            }

            return;
        }

        var flags = Default | Instance | Public | NonPublic;

        // Prefer direct HTML navigation if available.
        var navigateToString = webViewType.GetMethod("NavigateToString", flags, null, [typeof(string)], null);
        if (navigateToString != null)
        {
            try
            {
                navigateToString.Invoke(webView, [_pendingHtml]);
                _ = RestoreReadingProgressAsync(_subscribedViewModel?.CurrentChapterProgress ?? 0.0);
                Log.Information("RenderHtml path: reflection NavigateToString(string)");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RenderHtml failed in reflection NavigateToString(string). Error: {Error}",
                    ex.FullMessage());
                throw;
            }

            return;
        }

        // Second fallback: navigate to a local temp html file.
        var tempFile = Path.Combine(Path.GetTempPath(), $"epub-reader-{Guid.NewGuid():N}.html");
        File.WriteAllText(tempFile, _pendingHtml);
        var fileUri = new Uri(tempFile);
        Log.Debug("RenderHtml temp file created: {TempFile}", tempFile);

        var navigate = webViewType.GetMethod("Navigate", flags, null, [typeof(string)], null);
        if (navigate != null)
        {
            try
            {
                navigate.Invoke(webView, [fileUri.AbsoluteUri]);
                Log.Information("RenderHtml path: reflection Navigate(string), uri={Uri}", fileUri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RenderHtml failed in reflection Navigate(string), uri={Uri}. Error: {Error}",
                    fileUri.AbsoluteUri, ex.FullMessage());
                throw;
            }

            return;
        }

        // Some implementations use Url instead of Source.
        var urlProperty = webViewType.GetProperty("Url", flags);
        if (urlProperty?.CanWrite == true)
        {
            try
            {
                if (urlProperty.PropertyType == typeof(Uri))
                {
                    urlProperty.SetValue(webView, fileUri);
                    Log.Information("RenderHtml path: Url(Uri), uri={Uri}", fileUri.AbsoluteUri);
                }
                else
                {
                    urlProperty.SetValue(webView, fileUri.AbsoluteUri);
                    Log.Information("RenderHtml path: Url(string), uri={Uri}", fileUri.AbsoluteUri);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RenderHtml failed setting Url property, uri={Uri}. Error: {Error}", fileUri.AbsoluteUri,
                    ex.FullMessage());
                throw;
            }

            return;
        }

        var sourceProperty = webViewType.GetProperty("Source", flags);
        if (sourceProperty?.CanWrite != true)
        {
            Log.Warning(
                "Failed to render chapter HTML: WebView exposes no writable Url/Source property and no Navigate methods.");
            return;
        }

        var sourceType = sourceProperty.PropertyType;
        try
        {
            if (sourceType == typeof(Uri))
            {
                sourceProperty.SetValue(webView, fileUri);
                Log.Information("RenderHtml path: Source(Uri), uri={Uri}", fileUri.AbsoluteUri);
                return;
            }

            sourceProperty.SetValue(webView, fileUri.AbsoluteUri);
            Log.Information("RenderHtml path: Source(string), uri={Uri}", fileUri.AbsoluteUri);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RenderHtml failed setting Source property, uri={Uri}. Error: {Error}", fileUri.AbsoluteUri,
                ex.FullMessage());
            throw;
        }
    }

    private bool TryNavigateViaTempFile(object webView, Type webViewType, string html, int renderVersion)
    {
        var flags = Default | Instance | Public | NonPublic;
        var tempFile = Path.Combine(Path.GetTempPath(), $"epub-reader-{Guid.NewGuid():N}.html");
        File.WriteAllText(tempFile, html);
        var fileUri = new Uri(tempFile);

        Log.Debug("RenderHtml temp file created: {TempFile}", tempFile);

        var navigateUri = webViewType.GetMethod("Navigate", flags, null, [typeof(Uri)], null);
        if (navigateUri != null)
            try
            {
                navigateUri.Invoke(webView, [fileUri]);
                Log.Information("RenderHtml path: [Windows preferred] reflection Navigate(Uri), uri={Uri}",
                    fileUri.AbsoluteUri);
                ScheduleNavigationRetry(navigateUri, webView, fileUri, renderVersion);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "RenderHtml failed in [Windows preferred] reflection Navigate(Uri), uri={Uri}. Error: {Error}",
                    fileUri.AbsoluteUri, ex.FullMessage());
            }

        var navigateString = webViewType.GetMethod("Navigate", flags, null, [typeof(string)], null);
        if (navigateString != null)
            try
            {
                navigateString.Invoke(webView, [fileUri.AbsoluteUri]);
                Log.Information("RenderHtml path: [Windows preferred] reflection Navigate(string), uri={Uri}",
                    fileUri.AbsoluteUri);
                ScheduleNavigationRetry(navigateString, webView, fileUri.AbsoluteUri, renderVersion);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "RenderHtml failed in [Windows preferred] reflection Navigate(string), uri={Uri}. Error: {Error}",
                    fileUri.AbsoluteUri, ex.FullMessage());
            }

        var sourceProperty = webViewType.GetProperty("Source", flags);
        if (sourceProperty?.CanWrite == true)
            try
            {
                if (sourceProperty.PropertyType == typeof(Uri))
                {
                    sourceProperty.SetValue(webView, fileUri);
                    Log.Information("RenderHtml path: [Windows preferred] Source(Uri), uri={Uri}", fileUri.AbsoluteUri);
                    SchedulePropertyRetry(sourceProperty, webView, fileUri, renderVersion);
                }
                else
                {
                    sourceProperty.SetValue(webView, fileUri.AbsoluteUri);
                    Log.Information("RenderHtml path: [Windows preferred] Source(string), uri={Uri}",
                        fileUri.AbsoluteUri);
                    SchedulePropertyRetry(sourceProperty, webView, fileUri.AbsoluteUri, renderVersion);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RenderHtml failed in [Windows preferred] Source property, uri={Uri}. Error: {Error}",
                    fileUri.AbsoluteUri, ex.FullMessage());
            }

        var urlProperty = webViewType.GetProperty("Url", flags);
        if (urlProperty?.CanWrite == true)
            try
            {
                if (urlProperty.PropertyType == typeof(Uri))
                {
                    urlProperty.SetValue(webView, fileUri);
                    Log.Information("RenderHtml path: [Windows preferred] Url(Uri), uri={Uri}", fileUri.AbsoluteUri);
                    SchedulePropertyRetry(urlProperty, webView, fileUri, renderVersion);
                }
                else
                {
                    urlProperty.SetValue(webView, fileUri.AbsoluteUri);
                    Log.Information("RenderHtml path: [Windows preferred] Url(string), uri={Uri}", fileUri.AbsoluteUri);
                    SchedulePropertyRetry(urlProperty, webView, fileUri.AbsoluteUri, renderVersion);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RenderHtml failed in [Windows preferred] Url property, uri={Uri}. Error: {Error}",
                    fileUri.AbsoluteUri, ex.FullMessage());
            }

        return false;
    }

    private void ScheduleNavigationRetry(MethodInfo navigateMethod, object webView, object value, int renderVersion)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (renderVersion != _renderVersion)
            {
                Log.Debug("RenderHtml retry skipped: stale render version {RenderVersion} (latest={LatestVersion})",
                    renderVersion, _renderVersion);
                return;
            }

            try
            {
                navigateMethod.Invoke(webView, [value]);
                Log.Debug("RenderHtml retry navigation succeeded");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "RenderHtml retry navigation failed. Error: {Error}", ex.FullMessage());
            }
        }, DispatcherPriority.Background);
    }

    private void SchedulePropertyRetry(PropertyInfo property, object webView, object value, int renderVersion)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (renderVersion != _renderVersion)
            {
                Log.Debug("RenderHtml retry skipped: stale render version {RenderVersion} (latest={LatestVersion})",
                    renderVersion, _renderVersion);
                return;
            }

            try
            {
                property.SetValue(webView, value);
                Log.Debug("RenderHtml retry property set succeeded for {PropertyName}", property.Name);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "RenderHtml retry property set failed for {PropertyName}. Error: {Error}", property.Name,
                    ex.FullMessage());
            }
        }, DispatcherPriority.Background);
    }

    private Control? ResolveWebView()
    {
        if (_resolvedWebView != null) return _resolvedWebView;

        if (ChapterWebView != null)
        {
            _resolvedWebView = ChapterWebView;
            _resolvedTypedWebView = ChapterWebView;
            return _resolvedWebView;
        }

        var resolved = this.FindControl<Control>("ChapterWebView");
        if (resolved != null)
        {
            _resolvedWebView = resolved;
            _resolvedTypedWebView = resolved as IWebViewControl;
            Log.Debug("Resolved ChapterWebView via FindControl. Type={WebViewType}", resolved.GetType().FullName);
        }

        return resolved;
    }

    private ScrollViewer? ResolveTocScrollViewer()
    {
        return this.FindControl<ScrollViewer>("TocScrollViewer");
    }

    private TreeView? ResolveTocTreeView()
    {
        return this.FindControl<TreeView>("TocTreeView");
    }

    private static string WrapAsDocument(string rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
            return """
                   <html>
                     <head><meta charset="utf-8" /></head>
                     <body style="font-family:Segoe UI,sans-serif;padding:20px;">
                       <p>No content available.</p>
                     </body>
                   </html>
                   """;

        if (rawHtml.Contains("<html", StringComparison.OrdinalIgnoreCase)) return EnsureReadableDocument(rawHtml);

        return $$"""
                 <html>
                   <head>
                     <meta charset="utf-8" />
                     <base href="file:///" />
                   </head>
                   <body style="margin:0;padding:16px;">
                     {{rawHtml}}
                   </body>
                 </html>
                 """;
    }

    private static string EnsureReadableDocument(string htmlDocument)
    {
        const string readerBaseMeta = """
                                      <meta charset="utf-8" />
                                      <base href="file:///" />
                                      """;

        var headCloseIndex = htmlDocument.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (headCloseIndex >= 0) return htmlDocument.Insert(headCloseIndex, readerBaseMeta);

        var htmlOpenClose = htmlDocument.IndexOf('>');
        if (htmlOpenClose >= 0 && htmlDocument.Contains("<html", StringComparison.OrdinalIgnoreCase))
            return htmlDocument.Insert(htmlOpenClose + 1, $"<head>{readerBaseMeta}</head>");

        return $$"""
                 <html>
                   <head>
                     {{readerBaseMeta}}
                   </head>
                   <body>
                     {{htmlDocument}}
                   </body>
                 </html>
                 """;
    }

    private async void OnProgressPollTimerTick(object? sender, EventArgs e)
    {
        if (_isProgressPollInFlight) return;

        _isProgressPollInFlight = true;
        try
        {
            await PersistReadingProgressAsync(false);
        }
        finally
        {
            _isProgressPollInFlight = false;
        }
    }

    private async Task PersistReadingProgressAsync(bool force)
    {
        var vm = _subscribedViewModel;
        if (vm == null || vm.IsLoading) return;

        if (!force && DateTime.UtcNow < _suspendProgressSamplingUntilUtc) return;

        var progress = await ReadScrollProgressAsync();
        if (progress == null) return;

        try
        {
            await vm.UpdateReadingProgressAsync(progress.Value, force);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed persisting reading progress. Error: {Error}", ex.FullMessage());
        }
    }

    private async Task RestoreReadingProgressAsync(double progress)
    {
        if (progress <= 0.0) return;

        ResolveWebView();
        if (_resolvedTypedWebView is null) return;

        var clamped = Math.Clamp(progress, 0.0, 1.0);
        var script = $$"""
                       (function () {
                         var ratio = {{clamped.ToString(CultureInfo.InvariantCulture)}};
                         var doc = document.documentElement;
                         var body = document.body;
                         var viewport = window.innerHeight || 0;
                         var scrollHeight = Math.max(doc ? doc.scrollHeight : 0, body ? body.scrollHeight : 0);
                         var maxScroll = Math.max(scrollHeight - viewport, 0);
                         if (maxScroll <= 0) return;
                         window.scrollTo(0, maxScroll * ratio);
                       })();
                       """;

        try
        {
            await _resolvedTypedWebView.ExecuteScriptAsync(script);
            await Task.Delay(120);
            await _resolvedTypedWebView.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed restoring reading progress. Error: {Error}", ex.FullMessage());
        }
    }

    private async Task ApplySearchFocusAsync(SearchFocusRequestViewModel? request)
    {
        if (request == null || request.RequestId <= _lastAppliedSearchFocusRequestId) return;

        ResolveWebView();
        if (_resolvedTypedWebView is null || string.IsNullOrWhiteSpace(request.MatchText)) return;

        var escapedMatch = JsonSerializer.Serialize(request.MatchText);
        var script = $$"""
                       (function () {
                         var needle = {{escapedMatch}};
                         var expectedPos = {{request.Position}};
                         if (!needle) return "no-needle";

                         var existing = document.querySelectorAll("mark.epub-search-hit");
                         for (var i = 0; i < existing.length; i++) {
                           var node = existing[i];
                           var parent = node.parentNode;
                           if (!parent) continue;
                           parent.replaceChild(document.createTextNode(node.textContent || ""), node);
                           parent.normalize();
                         }

                         if (!document.getElementById("epub-search-hit-style")) {
                           var style = document.createElement("style");
                           style.id = "epub-search-hit-style";
                           style.textContent = "mark.epub-search-hit{background:#ffeb3b;color:#111;padding:0 1px;border-radius:2px;}";
                           (document.head || document.documentElement).appendChild(style);
                         }

                         var lowerNeedle = needle.toLowerCase();
                         var textOffset = 0;
                         var walker = document.createTreeWalker(
                           document.body || document.documentElement,
                           NodeFilter.SHOW_TEXT,
                           {
                             acceptNode: function (node) {
                               if (!node || !node.nodeValue) return NodeFilter.FILTER_REJECT;
                               var parent = node.parentElement;
                               if (!parent) return NodeFilter.FILTER_REJECT;
                               var tag = parent.tagName;
                               if (tag === "SCRIPT" || tag === "STYLE" || tag === "NOSCRIPT") return NodeFilter.FILTER_REJECT;
                               return NodeFilter.FILTER_ACCEPT;
                             }
                           });

                         var candidates = [];
                         var n;
                         while ((n = walker.nextNode())) {
                           var nodeText = n.nodeValue || "";
                           var lower = nodeText.toLowerCase();
                           var from = 0;
                           while (true) {
                             var idx = lower.indexOf(lowerNeedle, from);
                             if (idx < 0) break;
                             candidates.push({ node: n, localIndex: idx, globalIndex: textOffset + idx });
                             from = idx + 1;
                           }
                           textOffset += nodeText.length;
                         }

                         if (candidates.length === 0) return "not-found";

                         var best = null;
                         for (var j = 0; j < candidates.length; j++) {
                           var c = candidates[j];
                           if (c.globalIndex >= expectedPos) {
                             best = c;
                             break;
                           }
                         }

                         if (!best) {
                           var closestDiff = Number.MAX_SAFE_INTEGER;
                           for (var k = 0; k < candidates.length; k++) {
                             var current = candidates[k];
                             var diff = Math.abs(current.globalIndex - expectedPos);
                             if (diff < closestDiff) {
                               closestDiff = diff;
                               best = current;
                             }
                           }
                         }

                         if (!best || !best.node) return "not-found";

                         var range = document.createRange();
                         range.setStart(best.node, best.localIndex);
                         range.setEnd(best.node, best.localIndex + needle.length);

                         var mark = document.createElement("mark");
                         mark.className = "epub-search-hit";
                         range.surroundContents(mark);
                         mark.scrollIntoView({ behavior: "auto", block: "center", inline: "nearest" });
                         return "ok";
                       })();
                       """;

        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                var result = await _resolvedTypedWebView.ExecuteScriptAsync(script);
                if (result?.Contains("ok", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _lastAppliedSearchFocusRequestId = request.RequestId;
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed applying search focus on attempt {Attempt}. Error: {Error}", attempt + 1,
                    ex.FullMessage());
            }

            await Task.Delay(120);
        }
    }

    private async Task<double?> ReadScrollProgressAsync()
    {
        ResolveWebView();
        if (_resolvedTypedWebView is null) return null;

        const string script = """
                              (function () {
                                var doc = document.documentElement;
                                var body = document.body;
                                var scrollTop = window.scrollY || window.pageYOffset || (doc ? doc.scrollTop : 0) || (body ? body.scrollTop : 0) || 0;
                                var viewport = window.innerHeight || 0;
                                var scrollHeight = Math.max(doc ? doc.scrollHeight : 0, body ? body.scrollHeight : 0);
                                var maxScroll = Math.max(scrollHeight - viewport, 0);
                                if (maxScroll <= 0) return "0";
                                return String(Math.min(Math.max(scrollTop / maxScroll, 0), 1));
                              })();
                              """;

        try
        {
            var result = await _resolvedTypedWebView.ExecuteScriptAsync(script);
            if (string.IsNullOrWhiteSpace(result)) return null;

            var sanitized = result.Trim().Trim('"');
            return double.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? Math.Clamp(parsed, 0.0, 1.0)
                : null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed reading scroll progress from WebView. Error: {Error}", ex.FullMessage());
            return null;
        }
    }
}