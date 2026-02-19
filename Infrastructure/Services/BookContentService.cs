using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Application.DTOs.Book;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Common.Extensions;
using Infrastructure.Entities;
using Microsoft.Extensions.Logging;
using static System.Text.RegularExpressions.RegexOptions;

namespace Infrastructure.Services;

/// <summary>
///     Service for processing and preparing book content for display
/// </summary>
public partial class BookContentService(
    IBookRepository bookRepository,
    ICacheService cacheService,
    ILogger<BookContentService> logger) : IBookContentService
{
    private readonly IBookRepository _bookRepository =
        bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));

    private readonly ICacheService
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

    private readonly ILogger<BookContentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Gets processed chapter content ready for display
    /// </summary>
    public async Task<string> GetChapterContentAsync(
        Guid bookId,
        string chapterId,
        CssStyle? customStyle = null,
        CancellationToken cancellationToken = default)
    {
        var fragment = ExtractFragment(chapterId);
        var styleToUse = customStyle ?? CssStyle.Default;
        var styleCacheSegment = BuildStyleCacheSegment(styleToUse);
        var cacheKey = $"chapter-content:{bookId}:{chapterId}:{styleCacheSegment}";

        // Try to get from cache first
        var cachedContent = _cacheService.Get<string>(cacheKey);
        if (cachedContent != null) return cachedContent;

        var bookIdValue = BookId.From(bookId);
        var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);

        if (book == null) throw new BookNotFoundException(bookId);

        var chapter = book.GetChapterById(chapterId);
        if (chapter == null) throw new ChapterNotFoundException(bookId, chapterId);

        // Process the content
        var processedContent = ProcessChapterContent(book, chapter, styleToUse, fragment);

        // Cache the processed content
        _cacheService.Set(cacheKey, processedContent, TimeSpan.FromMinutes(30));

        return processedContent;
    }

    /// <summary>
    ///     Gets the complete book content as a single HTML document
    /// </summary>
    public async Task<string> GetCompleteBookContentAsync(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var bookIdValue = BookId.From(bookId);
        var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);

        if (book == null) throw new BookNotFoundException(bookId);

        var sb = new StringBuilder();
        sb.AppendLine($"<html><head><title>{book.Title}</title></head><body>");
        sb.AppendLine($"<h1>{book.Title}</h1>");
        sb.AppendLine($"<p>By: {string.Join(", ", book.Authors)}</p>");
        sb.AppendLine("<hr/>");

        foreach (var chapter in book.Chapters.OrderBy(c => c.Order))
        {
            sb.AppendLine($"<div class='chapter' id='{chapter.Id}'>");
            sb.AppendLine($"<h2>{chapter.Title}</h2>");
            sb.AppendLine(chapter.HtmlContent);
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    /// <summary>
    ///     Extracts plain text from a chapter
    /// </summary>
    public async Task<string> GetChapterPlainTextAsync(
        Guid bookId,
        string chapterId,
        CancellationToken cancellationToken = default)
    {
        var bookIdValue = BookId.From(bookId);
        var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);

        if (book == null) throw new BookNotFoundException(bookId);

        var chapter = book.GetChapterById(chapterId);
        return chapter == null ? throw new ChapterNotFoundException(bookId, chapterId) : chapter.GetPlainText();
    }

    /// <summary>
    ///     Gets statistics about a book
    /// </summary>
    public async Task<BookStatisticsDto> GetBookStatisticsAsync(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"book-stats:{bookId}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var bookIdValue = BookId.From(bookId);
                var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);

                if (book == null) throw new BookNotFoundException(bookId);

                var stats = new BookStatistics
                {
                    TotalChapters = book.Chapters.Count,
                    TotalWords = book.GetTotalWordCount(),
                    EstimatedReadingTimeMinutes = book.EstimateTotalReadingTime(),
                    HasCover = book.HasCover,
                    Language = book.Language,
                    Format = book.Metadata.Format.ToString()
                };

                // Map to DTO
                return new BookStatisticsDto
                {
                    TotalChapters = stats.TotalChapters,
                    TotalWords = stats.TotalWords,
                    EstimatedReadingTimeMinutes = stats.EstimatedReadingTimeMinutes,
                    HasCover = stats.HasCover,
                    Language = stats.Language,
                    Format = stats.Format
                };
            },
            TimeSpan.FromHours(1),
            cancellationToken);
    }

    /// <summary>
    ///     Cleans up and sanitizes HTML content
    /// </summary>
    public string SanitizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove script tags
        html = EventsRegex().Replace(html, string.Empty);

        // Remove event handlers
        html = TagsRegex().Replace(html, string.Empty);

        // Remove javascript: links
        html = LinksRegex().Replace(html, string.Empty);

        return html;
    }

    private static string ProcessChapterContent(Book book, Chapter chapter, CssStyle style, string? fragment)
    {
        var htmlContent = chapter.HtmlContent ?? string.Empty;
        htmlContent = NormalizeAnchorHeavyMarkup(htmlContent);
        htmlContent = InlineChapterImages(htmlContent, book.FilePath, chapter.Id);

        var cssInjection = $"<style>{style.ToStylesheet()}</style>";
        var anchorScrollScriptInjection = BuildAnchorScrollScript(fragment);
        var syntaxScriptInjection = """
                                    <script>
                                      (function () {
                                        if (window.__epubPyHl) return;
                                        window.__epubPyHl = true;

                                        var keywords = new Set([
                                          // Python
                                          "False","None","True","and","as","assert","async","await","break","class","continue",
                                          "def","del","elif","else","except","finally","for","from","global","if","import","in",
                                          "is","lambda","nonlocal","not","or","pass","raise","return","try","while","with","yield",
                                          "match","case",
                                          // JavaScript / TypeScript
                                          "function","let","const","var","switch","default","throw","new","this","typeof","instanceof",
                                          "extends","implements","interface","enum","private","public","protected","readonly","namespace",
                                          // C#
                                          "using","namespace","record","sealed","internal","virtual","override","static","void","base",
                                          "params","out","ref","where","struct","delegate","event","operator","checked","unchecked",
                                          // Java
                                          "package","synchronized","volatile","transient","final","abstract","native","strictfp","throws"
                                        ]);

                                        var builtins = new Set([
                                          // Python
                                          "int","str","dict","list","set","tuple","bool","float","bytes","object","type",
                                          "print","len","range","enumerate","map","filter","zip","sum","min","max","abs",
                                          "isinstance","getattr","setattr","hasattr","Exception","ValueError",
                                          // JavaScript / TypeScript
                                          "console","Promise","Array","Date","Math","JSON","RegExp","Set","Map","Error",
                                          // C#
                                          "string","int","long","double","decimal","bool","var","Task","List","Dictionary",
                                          "DateTime","Console","HttpClient","IEnumerable","IDisposable","Exception",
                                          // Java
                                          "String","Integer","Long","Double","Boolean","List","Map","Set","ArrayList","HashMap",
                                          "System","Exception"
                                        ]);

                                        function esc(s) {
                                          return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                                        }

                                        function isIdentStart(ch) {
                                          return /[A-Za-z_]/.test(ch);
                                        }

                                        function isIdent(ch) {
                                          return /[A-Za-z0-9_]/.test(ch);
                                        }

                                        function nextNonSpace(text, i) {
                                          while (i < text.length && /\s/.test(text[i])) i++;
                                          return i < text.length ? text[i] : "";
                                        }

                                        function pushToken(out, cls, text) {
                                          if (!text) return;
                                          if (!cls) {
                                            out.push(esc(text));
                                            return;
                                          }
                                          out.push("<span class=\"" + cls + "\">" + esc(text) + "</span>");
                                        }

                                        function inferSymbols(raw) {
                                          var inferredClasses = new Set();
                                          var inferredFunctions = new Set();
                                          var inferredMethods = new Set();
                                          var inferredDecorators = new Set();

                                          var lines = raw.split(/\r?\n/);
                                          for (var li = 0; li < lines.length; li++) {
                                            var line = lines[li];
                                            var trimmed = line.trim();
                                            if (!trimmed) continue;

                                            var mClass = trimmed.match(/^class\s+([A-Za-z_][A-Za-z0-9_]*)\b/);
                                            if (mClass) inferredClasses.add(mClass[1]);

                                            var mDef = trimmed.match(/^(?:async\s+)?def\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(/);
                                            if (mDef) inferredFunctions.add(mDef[1]);

                                            var mFnJs = trimmed.match(/^(?:export\s+)?(?:async\s+)?function\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(/);
                                            if (mFnJs) inferredFunctions.add(mFnJs[1]);

                                            var mFnTyped = trimmed.match(/^(?:public|private|protected|internal|static|final|virtual|override|abstract|\s)+\s*(?:[A-Za-z_][A-Za-z0-9_<>\[\]\.?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(/);
                                            if (mFnTyped) inferredFunctions.add(mFnTyped[1]);

                                            var mImportFrom = trimmed.match(/^from\s+[A-Za-z0-9_\.]+\s+import\s+(.+)$/);
                                            if (mImportFrom) {
                                              var imported = mImportFrom[1].split(",");
                                              for (var ii = 0; ii < imported.length; ii++) {
                                                var token = imported[ii].trim().split(/\s+as\s+/i)[0].trim();
                                                if (!token) continue;
                                                if (/^[A-Z]/.test(token)) inferredClasses.add(token);
                                                else inferredFunctions.add(token);
                                              }
                                            }

                                            var mImport = trimmed.match(/^import\s+(.+)$/);
                                            if (mImport) {
                                              var modules = mImport[1].split(",");
                                              for (var mi = 0; mi < modules.length; mi++) {
                                                var mod = modules[mi].trim().split(/\s+as\s+/i)[0].trim();
                                                if (!mod) continue;
                                                var last = mod.split(".").pop();
                                                if (last && /^[A-Z]/.test(last)) inferredClasses.add(last);
                                              }
                                            }

                                            var mDecorator = trimmed.match(/^@([A-Za-z_][A-Za-z0-9_\.]*)/);
                                            if (mDecorator) {
                                              inferredDecorators.add(mDecorator[1]);
                                            }
                                          }

                                          var methodMatches = raw.match(/\.[A-Za-z_][A-Za-z0-9_]*(?=\s*\()/g) || [];
                                          for (var mm = 0; mm < methodMatches.length; mm++) {
                                            inferredMethods.add(methodMatches[mm].slice(1));
                                          }

                                          return {
                                            classes: inferredClasses,
                                            functions: inferredFunctions,
                                            methods: inferredMethods,
                                            decorators: inferredDecorators
                                          };
                                        }

                                        function highlightText(raw) {
                                          var out = [];
                                          var i = 0;
                                          var expectFnName = false;
                                          var expectClassName = false;
                                          var prevSignificant = "";
                                          var inferred = inferSymbols(raw);

                                          while (i < raw.length) {
                                            var ch = raw[i];

                                            if (ch === "#" ) {
                                              var j = i + 1;
                                              while (j < raw.length && raw[j] !== "\n") j++;
                                              pushToken(out, "code-com", raw.slice(i, j));
                                              i = j;
                                              continue;
                                            }

                                            if (ch === "/" && raw[i + 1] === "*") {
                                              var j = i + 2;
                                              while (j + 1 < raw.length && !(raw[j] === "*" && raw[j + 1] === "/")) j++;
                                              j = Math.min(j + 2, raw.length);
                                              pushToken(out, "code-com", raw.slice(i, j));
                                              i = j;
                                              continue;
                                            }

                                            if (ch === "-" && raw[i + 1] === "-") {
                                              var j = i + 2;
                                              while (j < raw.length && raw[j] !== "\n") j++;
                                              pushToken(out, "code-com", raw.slice(i, j));
                                              i = j;
                                              continue;
                                            }

                                            if (ch === "\"" || ch === "'") {
                                              var q = ch;
                                              var j = i + 1;
                                              var triple = raw[j] === q && raw[j + 1] === q;
                                              if (triple) j += 2;
                                              while (j < raw.length) {
                                                if (raw[j] === "\\" && !triple) { j += 2; continue; }
                                                if (triple) {
                                                  if (raw[j] === q && raw[j + 1] === q && raw[j + 2] === q) { j += 3; break; }
                                                  j++;
                                                  continue;
                                                }
                                                if (raw[j] === q) { j++; break; }
                                                j++;
                                              }
                                              pushToken(out, "code-str", raw.slice(i, j));
                                              i = j;
                                              prevSignificant = "str";
                                              continue;
                                            }

                                            if (/\d/.test(ch)) {
                                              var j = i + 1;
                                              while (j < raw.length && /[\d_]/.test(raw[j])) j++;
                                              if (raw[j] === "." && /\d/.test(raw[j + 1])) {
                                                j++;
                                                while (j < raw.length && /[\d_]/.test(raw[j])) j++;
                                              }
                                              pushToken(out, "code-num", raw.slice(i, j));
                                              i = j;
                                              prevSignificant = "num";
                                              continue;
                                            }

                                            if (ch === "@" && isIdentStart(raw[i + 1] || "")) {
                                              var j = i + 2;
                                              while (j < raw.length && /[A-Za-z0-9_\.]/.test(raw[j])) j++;
                                              pushToken(out, "code-dec", raw.slice(i, j));
                                              i = j;
                                              prevSignificant = "dec";
                                              continue;
                                            }

                                            if (isIdentStart(ch)) {
                                              var j = i + 1;
                                              while (j < raw.length && isIdent(raw[j])) j++;
                                              var word = raw.slice(i, j);
                                              var cls = "";

                                              if (expectFnName) {
                                                cls = "code-fn";
                                                expectFnName = false;
                                              } else if (expectClassName) {
                                                cls = "code-cls";
                                                expectClassName = false;
                                              } else if (keywords.has(word)) {
                                                cls = "code-kw";
                                                if (word === "def") expectFnName = true;
                                                if (word === "class") expectClassName = true;
                                              } else if (inferred.classes.has(word)) {
                                                cls = "code-cls";
                                              } else if (inferred.functions.has(word)) {
                                                cls = "code-fn";
                                              } else if (builtins.has(word)) {
                                                cls = "code-builtin";
                                              } else if (prevSignificant === ".") {
                                                cls = "code-method";
                                              } else if (inferred.methods.has(word)) {
                                                cls = "code-method";
                                              } else if (nextNonSpace(raw, j) === "(") {
                                                cls = "code-fn";
                                              } else if (/^[A-Z][A-Za-z0-9_]*$/.test(word)) {
                                                cls = "code-cls";
                                              }

                                              pushToken(out, cls, word);
                                              i = j;
                                              if (cls !== "") prevSignificant = cls;
                                              continue;
                                            }

                                            if (/[\[\]\(\)\{\}\:\,\.\=\+\-\*\/%<>!]/.test(ch)) {
                                              var cls = ch === "." ? "" : "code-op";
                                              pushToken(out, cls, ch);
                                              i++;
                                              prevSignificant = ch;
                                              continue;
                                            }

                                            pushToken(out, "", ch);
                                            i++;
                                          }

                                          return out.join("");
                                        }

                                        function resolveHighlightTarget(node) {
                                          if (!node) return null;
                                          if (node.matches && node.matches("pre code")) return node;
                                          if (node.matches && node.matches("code.source-code")) return node;
                                          if (node.matches && node.matches("pre")) {
                                            var preCodeChild = node.querySelector ? node.querySelector("code") : null;
                                            return preCodeChild || node;
                                          }
                                          var codeChild = node.querySelector ? node.querySelector("code") : null;
                                          return codeChild || node;
                                        }

                                        function highlightNode(node) {
                                          var target = resolveHighlightTarget(node);
                                          if (!target || target.dataset.epubHlDone === "1") return;
                                          if (target.querySelector && target !== node && target.querySelector("*")) return;
                                          var raw = target.textContent || "";
                                          if (raw.length === 0 || raw.length > 100000) return;

                                          target.innerHTML = highlightText(raw);
                                          target.dataset.epubHlDone = "1";
                                        }

                                        function run() {
                                          var blocks = document.querySelectorAll("pre, pre code, code.source-code");
                                          for (var i = 0; i < blocks.length; i++) {
                                            highlightNode(blocks[i]);
                                          }
                                        }

                                        if (document.readyState === "loading") {
                                          document.addEventListener("DOMContentLoaded", run, { once: true });
                                        } else {
                                          run();
                                        }
                                      })();
                                    </script>
                                    """;

        if (htmlContent.Contains("<head>", StringComparison.OrdinalIgnoreCase))
            return htmlContent.Replace("</head>",
                cssInjection + syntaxScriptInjection + anchorScrollScriptInjection + "</head>");

        return cssInjection + syntaxScriptInjection + anchorScrollScriptInjection + htmlContent;
    }

    private static string? ExtractFragment(string? chapterId)
    {
        if (string.IsNullOrWhiteSpace(chapterId)) return null;

        var hashIndex = chapterId.IndexOf('#');
        if (hashIndex < 0 || hashIndex == chapterId.Length - 1) return null;

        var rawFragment = chapterId[(hashIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(rawFragment)) return null;

        return Uri.UnescapeDataString(rawFragment);
    }

    private static string BuildAnchorScrollScript(string? fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment)) return string.Empty;

        var safeFragment = fragment
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);

        return $$"""
                 <script>
                   (function () {
                     var anchorId = '{{safeFragment}}';
                     if (!anchorId) return;

                     function scrollToAnchor() {
                       var target = document.getElementById(anchorId);
                       if (!target) {
                         var byName = document.getElementsByName(anchorId);
                         target = byName && byName.length > 0 ? byName[0] : null;
                       }

                       if (target && target.scrollIntoView) {
                         target.scrollIntoView({ block: "start", inline: "nearest" });
                         return;
                       }

                       if (location.hash !== "#" + anchorId) {
                         location.hash = "#" + anchorId;
                       }
                     }

                     if (document.readyState === "loading") {
                       document.addEventListener("DOMContentLoaded", scrollToAnchor, { once: true });
                     } else {
                       scrollToAnchor();
                     }
                   })();
                 </script>
                 """;
    }

    private static string NormalizeAnchorHeavyMarkup(string html)
    {
        if (string.IsNullOrWhiteSpace(html) ||
            !html.Contains("<a", StringComparison.OrdinalIgnoreCase))
            return html;

        var tagRegex = new Regex(@"</?a\b[^>]*>", IgnoreCase | Compiled);
        var attrRegex = new Regex(@"(?<name>[^\s=/>]+)\s*=\s*(?<quote>[""'])(?<value>.*?)\k<quote>",
            IgnoreCase | Compiled);

        var output = new StringBuilder(html.Length + 256);
        var stack = new Stack<bool>();
        var cursor = 0;

        foreach (Match match in tagRegex.Matches(html))
        {
            output.Append(html, cursor, match.Index - cursor);
            cursor = match.Index + match.Length;

            var tag = match.Value;
            var isClosing = tag.StartsWith("</", StringComparison.Ordinal);
            if (isClosing)
            {
                var wasAllowed = stack.Count > 0 && stack.Pop();
                output.Append(wasAllowed ? "</a>" : "</span>");
                continue;
            }

            var isAllowed = IsLinkAllowedByClassOrId(tag, attrRegex);
            stack.Push(isAllowed);

            if (isAllowed)
            {
                output.Append(tag);
                continue;
            }

            var safeAttrs = BuildSafeSpanAttributes(tag, attrRegex);
            output.Append("<span");
            if (!string.IsNullOrWhiteSpace(safeAttrs))
            {
                output.Append(' ');
                output.Append(safeAttrs);
            }

            output.Append('>');
        }

        output.Append(html, cursor, html.Length - cursor);

        while (stack.Count > 0)
        {
            var wasAllowed = stack.Pop();
            output.Append(wasAllowed ? "</a>" : "</span>");
        }

        return output.ToString();
    }

    private static bool IsLinkAllowedByClassOrId(string tag, Regex attrRegex)
    {
        var classValue = string.Empty;
        var idValue = string.Empty;

        foreach (Match attr in attrRegex.Matches(tag))
        {
            var name = attr.Groups["name"].Value;
            var value = attr.Groups["value"].Value;

            if (name.Equals("class", StringComparison.OrdinalIgnoreCase))
                classValue = value;
            else if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) idValue = value;
        }

        return classValue.Contains("link", StringComparison.OrdinalIgnoreCase) ||
               idValue.Contains("link", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSafeSpanAttributes(string tag, Regex attrRegex)
    {
        var attrs = new List<string>();

        foreach (Match attr in attrRegex.Matches(tag))
        {
            var name = attr.Groups["name"].Value;
            if (name.Equals("href", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("target", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("rel", StringComparison.OrdinalIgnoreCase))
                continue;

            var quote = attr.Groups["quote"].Value;
            var value = attr.Groups["value"].Value;
            attrs.Add($"{name}={quote}{value}{quote}");
        }

        return string.Join(" ", attrs);
    }

    private static string InlineChapterImages(string html, string? epubFilePath, string chapterPath)
    {
        if (string.IsNullOrWhiteSpace(html) ||
            string.IsNullOrWhiteSpace(epubFilePath) ||
            !File.Exists(epubFilePath) ||
            !html.Contains("<img", StringComparison.OrdinalIgnoreCase))
            return html;

        var chapterPathWithoutAnchor = chapterPath.Split('#')[0].Trim();
        var chapterDirectory = Path.GetDirectoryName(chapterPathWithoutAnchor.Replace('\\', '/'))?.Replace('\\', '/')
                               ?? string.Empty;

        var srcRegex = new Regex(
            "<img\\b[^>]*\\bsrc\\s*=\\s*['\"](?<src>[^'\"]+)['\"][^>]*>",
            IgnoreCase | Compiled);

        try
        {
            using var archive = ZipFile.OpenRead(epubFilePath);
            var entriesByPath = archive.Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.FullName))
                .ToDictionary(e => e.FullName.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);

            return srcRegex.Replace(html, match =>
            {
                var srcValue = match.Groups["src"].Value;
                if (string.IsNullOrWhiteSpace(srcValue) || IsExternalOrEmbeddedSource(srcValue)) return match.Value;

                var relativePath = srcValue.Split('#')[0].Split('?')[0];
                var unescapedRelative = Uri.UnescapeDataString(relativePath);
                var resolvedPath = NormalizeEpubPath(
                    string.IsNullOrEmpty(chapterDirectory)
                        ? unescapedRelative
                        : $"{chapterDirectory}/{unescapedRelative}");

                if (!entriesByPath.TryGetValue(resolvedPath, out var entry)) return match.Value;

                using var stream = entry.Open();
                using var memory = new MemoryStream();
                stream.CopyTo(memory);

                var bytes = memory.ToArray();
                if (bytes.Length == 0) return match.Value;

                var mime = GuessImageMimeType(entry.FullName);
                var dataUri = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                return match.Value.Replace(srcValue, dataUri, StringComparison.Ordinal);
            });
        }
        catch
        {
            return html;
        }
    }

    private static bool IsExternalOrEmbeddedSource(string srcValue)
    {
        return srcValue.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
               || srcValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || srcValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || srcValue.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
               || srcValue.StartsWith("cid:", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEpubPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        while (normalized.Contains("//", StringComparison.Ordinal))
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>();
        foreach (var part in parts)
        {
            if (part == ".") continue;

            if (part == "..")
            {
                if (stack.Count > 0) stack.Pop();
                continue;
            }

            stack.Push(part);
        }

        return string.Join("/", stack.Reverse());
    }

    private static string GuessImageMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private static string BuildStyleCacheSegment(CssStyle style)
    {
        var css = style.ToStylesheet();
        var bytes = Encoding.UTF8.GetBytes(css);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }

    [GeneratedRegex("""\s(on\w+)\s*=\s*["'][^"']*["']""", IgnoreCase, "en-US")]
    private static partial Regex TagsRegex();

    [GeneratedRegex(@"<script[^>]*>.*?</script>", None | IgnoreCase | Singleline, "en-US")]
    private static partial Regex EventsRegex();

    [GeneratedRegex(@"javascript:", IgnoreCase, "en-US")]
    private static partial Regex LinksRegex();
}