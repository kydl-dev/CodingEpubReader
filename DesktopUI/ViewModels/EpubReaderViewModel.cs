using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Books.ExportCompleteBook;
using Application.UseCases.Books.GetChapterContent;
using Application.UseCases.Books.GetChapterPlainText;
using Application.UseCases.Navigation.GetTableOfContents;
using Application.UseCases.Navigation.NavigateToChapter;
using Application.UseCases.ReadingProgress.UpdateReadingProgress;
using Application.UseCases.Search.GetSearchSuggestions;
using Application.UseCases.Search.SearchInBook;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using ReactiveUI;
using Serilog;
using Shared.Exceptions;
using Unit = System.Reactive.Unit;

namespace DesktopUI.ViewModels;

public class EpubReaderViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly IMediator _mediator;
    private readonly string? _preferredInitialChapterId;
    private readonly double _preferredInitialProgress;
    private readonly Dictionary<string, Guid> _readerStyleIdsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ISavedCssStyleRepository _savedCssStyleRepository;
    private readonly IThemeService _themeService;
    private Guid? _activeReaderStyleId;
    private ObservableCollection<string> _appThemeOptions;
    private bool _areStylesLoaded;

    private Guid _bookId;
    private string _bookTitle;
    private int _chapterCount;
    private TocNodeViewModel? _currentChapter;
    private string _currentChapterContent;
    private double _currentChapterProgress;
    private bool _hasAppliedPreferredProgress;
    private bool _isApplyingSelections;
    private bool _isLoading;
    private bool _isSearchBusy;
    private bool _isSearchVisible;
    private bool _isThemePanelVisible;
    private bool _isTocVisible;
    private string? _lastPersistedChapterId;
    private double _lastPersistedProgress = -1;
    private ObservableCollection<string> _readerStyleOptions;
    private bool _searchCaseSensitive;
    private SearchFocusRequestViewModel? _searchFocusRequest;
    private int _searchFocusRequestId;
    private string _searchQuery;
    private ObservableCollection<SearchResultItemViewModel> _searchResults;
    private ObservableCollection<string> _searchSuggestions;
    private bool _searchWholeWord;
    private string _selectedAppTheme;
    private string _selectedReaderStyle;
    private string _statusMessage;
    private ObservableCollection<TocNodeViewModel> _tableOfContents;

    public EpubReaderViewModel(
        IMediator mediator,
        IBookContentService bookContentService,
        IThemeService themeService,
        ISavedCssStyleRepository savedCssStyleRepository,
        Guid bookId,
        string bookTitle,
        string? preferredInitialChapterId = null,
        double preferredInitialProgress = 0.0)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        BookContentService = bookContentService ?? throw new ArgumentNullException(nameof(bookContentService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _savedCssStyleRepository =
            savedCssStyleRepository ?? throw new ArgumentNullException(nameof(savedCssStyleRepository));

        _bookId = bookId;
        _bookTitle = bookTitle;
        _preferredInitialChapterId = preferredInitialChapterId;
        _preferredInitialProgress = Math.Clamp(preferredInitialProgress, 0.0, 1.0);
        _currentChapterContent = string.Empty;
        _statusMessage = "Loading book...";
        _tableOfContents = [];
        _searchSuggestions = [];
        _searchResults = [];
        _isTocVisible = true;
        _isSearchVisible = false;
        _isSearchBusy = false;
        _searchQuery = string.Empty;
        _chapterCount = 0;
        _isThemePanelVisible = false;
        _appThemeOptions = ["Dark", "Light", "Sepia"];
        _selectedAppTheme = _themeService.CurrentTheme.Kind.ToString();
        _readerStyleOptions = [];
        _selectedReaderStyle = string.Empty;
        _currentChapterProgress = _preferredInitialProgress;

        LoadBookCommand = ReactiveCommand.CreateFromTask(
            LoadBookAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        NavigateToChapterCommand = ReactiveCommand.CreateFromTask<TocNodeViewModel>(
            NavigateToChapterAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        ToggleTocCommand = ReactiveCommand.Create(ToggleToc);
        ToggleSearchCommand = ReactiveCommand.Create(ToggleSearch);
        SearchInBookCommand = ReactiveCommand.CreateFromTask(
            ExecuteSearchAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        OpenSearchResultCommand = ReactiveCommand.CreateFromTask<SearchResultItemViewModel>(
            OpenSearchResultAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        ToggleThemePanelCommand = ReactiveCommand.Create(ToggleThemePanel);
        SyncCodeDefaultsToDbCommand = ReactiveCommand.CreateFromTask(
            SyncCodeDefaultsToDbAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        CopyChapterTextCommand = ReactiveCommand.CreateFromTask(
            CopyChapterTextAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        ExportBookCommand = ReactiveCommand.CreateFromTask(
            ExportBookAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        this.WhenAnyValue(x => x.SearchQuery)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Where(query => !string.IsNullOrWhiteSpace(query) && query.Length >= 2)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async void (_) =>
            {
                try
                {
                    await LoadSearchSuggestionsAsync();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to load search suggestions, error: {Error}", e.FullMessage());
                }
            });

        this.WhenAnyValue(x => x.SelectedAppTheme)
            .Skip(1)
            .Where(theme => !_isApplyingSelections && !string.IsNullOrWhiteSpace(theme))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async themeName =>
            {
                try
                {
                    await ApplyAppThemeAsync(themeName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply app theme {ThemeName}, Error: {Error}", themeName, ex.FullMessage());
                }
            });

        this.WhenAnyValue(x => x.SelectedReaderStyle)
            .Skip(1)
            .Where(style => !_isApplyingSelections && !string.IsNullOrWhiteSpace(style))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async styleName =>
            {
                try
                {
                    await ApplyReaderStyleAsync(styleName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply reader style {StyleName}. Error: {Error}", styleName,
                        ex.FullMessage());
                }
            });

        Activator = new ViewModelActivator();

        this.WhenActivated(disposables =>
        {
            LoadBookCommand.Execute()
                .Subscribe()
                .DisposeWith(disposables);

            LoadBookCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error loading book: {ex.Message}";
                    Log.Error(ex, "Failed to load book {BookId}. Error: {Error}", _bookId, ex.FullMessage());
                })
                .DisposeWith(disposables);

            NavigateToChapterCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error loading chapter: {ex.Message}";
                    Log.Error(ex, "Failed to load chapter. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            CopyChapterTextCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error copying text: {ex.Message}";
                    Log.Error(ex, "Failed to copy chapter text. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            ExportBookCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error exporting book: {ex.Message}";
                    Log.Error(ex, "Failed to export book. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            SearchInBookCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Search failed: {ex.Message}";
                    Log.Error(ex, "Search failed. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            SyncCodeDefaultsToDbCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error syncing code defaults: {ex.Message}";
                    Log.Error(ex, "Failed syncing code defaults to DB. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);
        });
    }

    public IBookContentService BookContentService { get; }

    public Guid BookId
    {
        get => _bookId;
        set => this.RaiseAndSetIfChanged(ref _bookId, value);
    }

    public string BookTitle
    {
        get => _bookTitle;
        set => this.RaiseAndSetIfChanged(ref _bookTitle, value);
    }

    public string CurrentChapterContent
    {
        get => _currentChapterContent;
        set => this.RaiseAndSetIfChanged(ref _currentChapterContent, value);
    }

    public TocNodeViewModel? CurrentChapter
    {
        get => _currentChapter;
        set => this.RaiseAndSetIfChanged(ref _currentChapter, value);
    }

    public ObservableCollection<TocNodeViewModel> TableOfContents
    {
        get => _tableOfContents;
        set => this.RaiseAndSetIfChanged(ref _tableOfContents, value);
    }

    public int ChapterCount
    {
        get => _chapterCount;
        set => this.RaiseAndSetIfChanged(ref _chapterCount, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsTocVisible
    {
        get => _isTocVisible;
        set => this.RaiseAndSetIfChanged(ref _isTocVisible, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => this.RaiseAndSetIfChanged(ref _searchQuery, value);
    }

    public ObservableCollection<string> SearchSuggestions
    {
        get => _searchSuggestions;
        set => this.RaiseAndSetIfChanged(ref _searchSuggestions, value);
    }

    public ObservableCollection<SearchResultItemViewModel> SearchResults
    {
        get => _searchResults;
        set => this.RaiseAndSetIfChanged(ref _searchResults, value);
    }

    public SearchFocusRequestViewModel? SearchFocusRequest
    {
        get => _searchFocusRequest;
        private set => this.RaiseAndSetIfChanged(ref _searchFocusRequest, value);
    }

    public bool IsSearchVisible
    {
        get => _isSearchVisible;
        set => this.RaiseAndSetIfChanged(ref _isSearchVisible, value);
    }

    public bool IsSearchBusy
    {
        get => _isSearchBusy;
        set => this.RaiseAndSetIfChanged(ref _isSearchBusy, value);
    }

    public bool SearchCaseSensitive
    {
        get => _searchCaseSensitive;
        set => this.RaiseAndSetIfChanged(ref _searchCaseSensitive, value);
    }

    public bool SearchWholeWord
    {
        get => _searchWholeWord;
        set => this.RaiseAndSetIfChanged(ref _searchWholeWord, value);
    }

    public bool IsThemePanelVisible
    {
        get => _isThemePanelVisible;
        set => this.RaiseAndSetIfChanged(ref _isThemePanelVisible, value);
    }

    public ObservableCollection<string> AppThemeOptions
    {
        get => _appThemeOptions;
        set => this.RaiseAndSetIfChanged(ref _appThemeOptions, value);
    }

    public string SelectedAppTheme
    {
        get => _selectedAppTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedAppTheme, value);
    }

    public ObservableCollection<string> ReaderStyleOptions
    {
        get => _readerStyleOptions;
        set => this.RaiseAndSetIfChanged(ref _readerStyleOptions, value);
    }

    public string SelectedReaderStyle
    {
        get => _selectedReaderStyle;
        set => this.RaiseAndSetIfChanged(ref _selectedReaderStyle, value);
    }

    public double CurrentChapterProgress
    {
        get => _currentChapterProgress;
        private set => this.RaiseAndSetIfChanged(ref _currentChapterProgress, value);
    }

    public bool IsDebugToolsVisible => Debugger.IsAttached || IsDebugBuild;

    private static bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    private ReactiveCommand<Unit, Unit> LoadBookCommand { get; }
    public ReactiveCommand<TocNodeViewModel, Unit> NavigateToChapterCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleTocCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleSearchCommand { get; }
    public ReactiveCommand<Unit, Unit> SearchInBookCommand { get; }
    public ReactiveCommand<SearchResultItemViewModel, Unit> OpenSearchResultCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleThemePanelCommand { get; }
    public ReactiveCommand<Unit, Unit> SyncCodeDefaultsToDbCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyChapterTextCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportBookCommand { get; }

    public ViewModelActivator Activator { get; }

    private async Task LoadBookAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading table of contents...";

            await EnsureReaderStylesLoadedAsync();
            SyncThemeSelectionFromService();

            var query = new GetTableOfContentsQuery(BookId);
            var toc = await _mediator.Send(query);
            var tocTree = toc.Select(item => new TocNodeViewModel(item)).ToList();

            if (tocTree.Count == 1 &&
                tocTree[0].IsSectionHeader &&
                string.Equals(tocTree[0].Title.Trim(), "Contents", StringComparison.OrdinalIgnoreCase))
                tocTree = tocTree[0].Children.ToList();

            TableOfContents.Clear();
            foreach (var item in tocTree) TableOfContents.Add(item);

            ChapterCount = TableOfContents
                .SelectMany(FlattenToc)
                .Count(node => node.HasContent);

            var navigableChapters = TableOfContents
                .SelectMany(FlattenToc)
                .Where(node => node.HasContent)
                .ToList();

            if (navigableChapters.Count > 0)
            {
                var loaded = await TryLoadInitialChapterAsync(navigableChapters);
                if (!loaded) StatusMessage = "No readable chapters found";
            }
            else
            {
                StatusMessage = "No chapters found";
            }

            Log.Information("Loaded book {BookId} with {ChapterCount} chapters", BookId, ChapterCount);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading book: {ex.Message}";
            Log.Error(ex, "Failed to load book {BookId}. Error: {Error}", BookId, ex.FullMessage());
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task NavigateToChapterAsync(TocNodeViewModel chapter)
    {
        try
        {
            if (!chapter.HasContent)
            {
                StatusMessage = $"Cannot navigate to '{chapter.Title}' because it has no chapter link.";
                Log.Warning(
                    "Ignored non-navigable TOC item for book {BookId}: Title='{Title}', Id='{TocId}'",
                    BookId,
                    chapter.Title,
                    chapter.Id);
                return;
            }

            IsLoading = true;
            StatusMessage = $"Loading chapter: {chapter.Title}...";
            CurrentChapter = chapter;

            // Step 1 — NavigateToChapterCommand: validates the chapter exists on the Book
            // aggregate and persists the new reading position to the DB.
            await _mediator.Send(new NavigateToChapterCommand(BookId, chapter.ContentSrc.Trim()));

            // Step 2 — GetChapterContentQuery: returns the fully styled HTML for the WebView
            // (applies the active CSS reader style via IBookContentService).
            var contentQuery = new GetChapterContentQuery(BookId, chapter.ContentSrc.Trim(), _activeReaderStyleId);
            CurrentChapterContent = await _mediator.Send(contentQuery);

            if (!_hasAppliedPreferredProgress &&
                !string.IsNullOrWhiteSpace(_preferredInitialChapterId) &&
                ChapterIdsEquivalent(chapter.ContentSrc, _preferredInitialChapterId))
            {
                CurrentChapterProgress = _preferredInitialProgress;
                _hasAppliedPreferredProgress = true;
            }
            else
            {
                CurrentChapterProgress = 0.0;
            }

            StatusMessage = $"Reading: {chapter.Title}";
            Log.Information("Navigated to chapter {ChapterId} for book {BookId}", chapter.ContentSrc, BookId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading chapter: {ex.Message}";
            Log.Error(ex, "Failed to load chapter {ChapterId}. Error: {Error}", chapter.ContentSrc, ex.FullMessage());
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<bool> TryLoadInitialChapterAsync(IReadOnlyList<TocNodeViewModel> chapters)
    {
        var candidates = new List<TocNodeViewModel>();

        if (!string.IsNullOrWhiteSpace(_preferredInitialChapterId))
        {
            var preferred =
                chapters.FirstOrDefault(c => ChapterIdsEquivalent(c.ContentSrc, _preferredInitialChapterId));
            if (preferred != null)
                candidates.Add(preferred);
            else
                // Some books (including many with dedicated cover spine items) don't include
                // the first spine chapter in TOC. Still prefer opening that real first chapter.
                candidates.Add(new TocNodeViewModel(new TocItemDto(
                    _preferredInitialChapterId,
                    "Cover",
                    _preferredInitialChapterId,
                    -1,
                    0,
                    [])));
        }

        var coverCandidate = chapters.FirstOrDefault(c =>
            c.ContentSrc.Contains("cover", StringComparison.OrdinalIgnoreCase) ||
            c.Title.Contains("cover", StringComparison.OrdinalIgnoreCase));

        if (coverCandidate != null && !candidates.Contains(coverCandidate)) candidates.Add(coverCandidate);

        foreach (var chapter in chapters.Where(chapter => !candidates.Contains(chapter))) candidates.Add(chapter);

        foreach (var candidate in candidates)
            try
            {
                await NavigateToChapterAsync(candidate);
                if (!string.IsNullOrWhiteSpace(CurrentChapterContent))
                {
                    if (!string.IsNullOrWhiteSpace(_preferredInitialChapterId) &&
                        ChapterIdsEquivalent(candidate.ContentSrc, _preferredInitialChapterId))
                    {
                        CurrentChapterProgress = _preferredInitialProgress;
                        await UpdateReadingProgressAsync(_preferredInitialProgress, true);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    ex,
                    "Initial chapter candidate failed for book {BookId}: {ChapterId}. Error: {Error}",
                    BookId,
                    candidate.ContentSrc, ex.FullMessage());
            }

        return false;
    }

    private static bool ChapterIdsEquivalent(string first, string second)
    {
        static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var trimmed = value.Trim();
            var hashIndex = trimmed.IndexOf('#');
            if (hashIndex >= 0) trimmed = trimmed[..hashIndex];

            trimmed = trimmed.Replace('\\', '/');
            trimmed = trimmed.TrimStart('/');
            if (trimmed.StartsWith("./", StringComparison.Ordinal)) trimmed = trimmed[2..];

            return WebUtility.UrlDecode(trimmed);
        }

        var left = Normalize(first);
        var right = Normalize(second);

        return left.Equals(right, StringComparison.OrdinalIgnoreCase) ||
               left.EndsWith("/" + right, StringComparison.OrdinalIgnoreCase) ||
               right.EndsWith("/" + left, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<TocNodeViewModel> FlattenToc(TocNodeViewModel item)
    {
        yield return item;
        foreach (var child in item.Children)
        foreach (var descendant in FlattenToc(child))
            yield return descendant;
    }

    private void ToggleToc()
    {
        IsTocVisible = !IsTocVisible;
    }

    private void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
        if (IsSearchVisible) return;
        SearchQuery = string.Empty;
        SearchSuggestions.Clear();
        SearchResults.Clear();
    }

    private void ToggleThemePanel()
    {
        IsThemePanelVisible = !IsThemePanelVisible;
    }

    public async Task UpdateReadingProgressAsync(double progress, bool force = false)
    {
        if (CurrentChapter == null || !CurrentChapter.HasContent) return;

        var clampedProgress = Math.Clamp(progress, 0.0, 1.0);
        CurrentChapterProgress = clampedProgress;

        var chapterId = CurrentChapter.ContentSrc.Trim();
        var chapterChanged = !string.Equals(_lastPersistedChapterId, chapterId, StringComparison.OrdinalIgnoreCase);
        var delta = Math.Abs(clampedProgress - _lastPersistedProgress);

        if (!force && !chapterChanged && delta < 0.01) return;

        await _mediator.Send(new UpdateReadingProgressCommand(BookId, chapterId, clampedProgress));
        _lastPersistedChapterId = chapterId;
        _lastPersistedProgress = clampedProgress;
    }

    private async Task LoadSearchSuggestionsAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
            {
                SearchSuggestions.Clear();
                return;
            }

            var query = new GetSearchSuggestionsQuery(BookId, SearchQuery);
            var suggestions = await _mediator.Send(query);

            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions) SearchSuggestions.Add(suggestion);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load search suggestions. Error: {Error}", ex.FullMessage());
        }
    }

    private async Task ExecuteSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            StatusMessage = "Enter text to search.";
            return;
        }

        try
        {
            IsSearchBusy = true;
            StatusMessage = $"Searching for '{SearchQuery}'...";

            var query = new SearchInBookQuery(
                BookId,
                SearchQuery.Trim(),
                SearchCaseSensitive,
                SearchWholeWord);

            var results = await _mediator.Send(query);
            var mapped = results
                .OrderBy(r => r.ChapterOrder)
                .ThenBy(r => r.Position)
                .Select(r => new SearchResultItemViewModel
                {
                    ChapterId = r.ChapterId,
                    ChapterTitle = r.ChapterTitle,
                    ChapterOrder = r.ChapterOrder,
                    Position = r.Position,
                    MatchedText = r.MatchedText,
                    BeforeContext = r.BeforeContext,
                    AfterContext = r.AfterContext,
                    Preview = r.GetPreview()
                })
                .ToList();

            SearchResults.Clear();
            foreach (var item in mapped) SearchResults.Add(item);

            StatusMessage = SearchResults.Count == 0
                ? "No search matches found."
                : $"Found {SearchResults.Count} match(es).";
        }
        finally
        {
            IsSearchBusy = false;
        }
    }

    private async Task OpenSearchResultAsync(SearchResultItemViewModel? result)
    {
        if (result == null) return;

        var target = TableOfContents
            .SelectMany(FlattenToc)
            .FirstOrDefault(node => node.HasContent && ChapterIdsEquivalent(node.ContentSrc, result.ChapterId));

        if (target != null)
        {
            await NavigateToChapterAsync(target);
            SearchFocusRequest = new SearchFocusRequestViewModel
            {
                RequestId = ++_searchFocusRequestId,
                MatchText = result.MatchedText,
                Position = result.Position
            };
            return;
        }

        await NavigateToChapterAsync(new TocNodeViewModel(new TocItemDto(
            result.ChapterId,
            string.IsNullOrWhiteSpace(result.ChapterTitle) ? "Search Result" : result.ChapterTitle,
            result.ChapterId,
            result.ChapterOrder,
            0,
            [])));

        SearchFocusRequest = new SearchFocusRequestViewModel
        {
            RequestId = ++_searchFocusRequestId,
            MatchText = result.MatchedText,
            Position = result.Position
        };
    }

    private async Task EnsureReaderStylesLoadedAsync()
    {
        if (_areStylesLoaded) return;

        var styles = (await _savedCssStyleRepository.GetAllAsync()).ToList();

        if (styles.All(s => !string.Equals(s.Name, "Light", StringComparison.OrdinalIgnoreCase)))
            await _savedCssStyleRepository.AddAsync(SavedCssStyle.Create("Light", CssStyle.Default));

        if (styles.All(s => !string.Equals(s.Name, "Dracula", StringComparison.OrdinalIgnoreCase)))
            await _savedCssStyleRepository.AddAsync(SavedCssStyle.Create("Dracula", CssStyle.Dracula));

        if (styles.All(s => !string.Equals(s.Name, "Sepia", StringComparison.OrdinalIgnoreCase)))
            await _savedCssStyleRepository.AddAsync(SavedCssStyle.Create("Sepia", CssStyle.Sepia));

        styles = (await _savedCssStyleRepository.GetAllAsync()).ToList();

        var defaultStyle = styles.FirstOrDefault(s => s.IsDefault);
        if (defaultStyle == null)
        {
            var dracula =
                styles.FirstOrDefault(s => string.Equals(s.Name, "Dracula", StringComparison.OrdinalIgnoreCase));
            if (dracula != null)
            {
                await _savedCssStyleRepository.ClearAllDefaultsAsync();
                dracula.SetAsDefault();
                await _savedCssStyleRepository.UpdateAsync(dracula);
            }

            styles = (await _savedCssStyleRepository.GetAllAsync()).ToList();
            defaultStyle = styles.FirstOrDefault(s => s.IsDefault);
        }

        _readerStyleIdsByName.Clear();
        foreach (var style in styles) _readerStyleIdsByName[style.Name] = style.Id;

        var orderedNames = styles
            .Select(s => s.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name == "Dracula" ? 0 : name == "Light" ? 1 : name == "Sepia" ? 2 : 3)
            .ThenBy(name => name)
            .ToList();

        ReaderStyleOptions.Clear();
        foreach (var name in orderedNames) ReaderStyleOptions.Add(name);

        _isApplyingSelections = true;
        try
        {
            var initialStyleName = defaultStyle?.Name
                                   ?? orderedNames.FirstOrDefault()
                                   ?? "Dracula";

            SelectedReaderStyle = initialStyleName;
            if (_readerStyleIdsByName.TryGetValue(initialStyleName, out var styleId)) _activeReaderStyleId = styleId;

            SelectedAppTheme = _themeService.CurrentTheme.Kind.ToString();
        }
        finally
        {
            _isApplyingSelections = false;
        }

        _areStylesLoaded = true;
    }

    private void SyncThemeSelectionFromService()
    {
        _isApplyingSelections = true;
        try
        {
            SelectedAppTheme = _themeService.CurrentTheme.Kind.ToString();
        }
        finally
        {
            _isApplyingSelections = false;
        }
    }

    private async Task ApplyAppThemeAsync(string themeName)
    {
        if (!Enum.TryParse<ThemeKind>(themeName, true, out var kind)) return;

        await _themeService.SetThemeAsync(kind);
        StatusMessage = $"App theme set to {themeName}.";
    }

    private async Task ApplyReaderStyleAsync(string styleName)
    {
        await EnsureReaderStylesLoadedAsync();

        if (!_readerStyleIdsByName.TryGetValue(styleName, out var styleId)) return;

        var selectedStyle = await _savedCssStyleRepository.GetByIdAsync(styleId);
        if (selectedStyle == null) return;

        await _savedCssStyleRepository.ClearAllDefaultsAsync();
        selectedStyle.SetAsDefault();
        await _savedCssStyleRepository.UpdateAsync(selectedStyle);

        _activeReaderStyleId = selectedStyle.Id;
        StatusMessage = $"Reader style set to {selectedStyle.Name}.";

        await ReloadCurrentChapterContentAsync();
    }

    private async Task ReloadCurrentChapterContentAsync()
    {
        if (CurrentChapter == null || !CurrentChapter.HasContent) return;

        var chapterId = CurrentChapter.ContentSrc.Trim();
        var contentQuery = new GetChapterContentQuery(BookId, chapterId, _activeReaderStyleId);
        CurrentChapterContent = await _mediator.Send(contentQuery);
    }

    private async Task SyncCodeDefaultsToDbAsync()
    {
        if (!IsDebugToolsVisible) return;

        IsLoading = true;
        StatusMessage = "Syncing code defaults to DB...";

        try
        {
            var desiredStyles = new[]
            {
                (Name: "Light", Style: CssStyle.Default),
                (Name: "Dracula", Style: CssStyle.Dracula),
                (Name: "Sepia", Style: CssStyle.Sepia)
            };

            var existingStyles = (await _savedCssStyleRepository.GetAllAsync()).ToList();

            foreach (var desired in desiredStyles)
            {
                var existing = existingStyles.FirstOrDefault(s =>
                    string.Equals(s.Name, desired.Name, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    await _savedCssStyleRepository.AddAsync(SavedCssStyle.Create(desired.Name, desired.Style));
                    continue;
                }

                existing.UpdateStyle(desired.Style);
                await _savedCssStyleRepository.UpdateAsync(existing);
            }

            await _savedCssStyleRepository.ClearAllDefaultsAsync();
            var refreshed = (await _savedCssStyleRepository.GetAllAsync()).ToList();
            var dracula =
                refreshed.FirstOrDefault(s => string.Equals(s.Name, "Dracula", StringComparison.OrdinalIgnoreCase));
            if (dracula != null)
            {
                dracula.SetAsDefault();
                await _savedCssStyleRepository.UpdateAsync(dracula);
            }

            _areStylesLoaded = false;
            await EnsureReaderStylesLoadedAsync();

            await _themeService.SetThemeAsync(_themeService.CurrentTheme.Kind);
            SyncThemeSelectionFromService();

            if (CurrentChapter != null) await NavigateToChapterAsync(CurrentChapter);

            StatusMessage = "Code defaults synced to DB (debug).";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CopyChapterTextAsync()
    {
        if (CurrentChapter == null)
        {
            StatusMessage = "No chapter selected to copy";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Copying chapter text...";

            var query = new GetChapterPlainTextQuery(BookId, CurrentChapter.ContentSrc);
            var plainText = await _mediator.Send(query);

            var clipboard = GetClipboard();

            if (clipboard != null)
            {
                await clipboard.SetTextAsync(plainText);
                StatusMessage = $"Chapter text copied to clipboard ({plainText.Length} characters)";
                Log.Information("Copied chapter {ChapterId} text to clipboard", CurrentChapter.ContentSrc);
            }
            else
            {
                StatusMessage = "Clipboard not available (Window may not be active)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying text: {ex.Message}";
            Log.Error(ex, "Failed to copy chapter text. Error: {Error}", ex.FullMessage());
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static IClipboard? GetClipboard()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow?.Clipboard;

        return null;
    }

    private async Task ExportBookAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Exporting book...";

            var query = new ExportCompleteBookQuery(BookId);
            var htmlContent = await _mediator.Send(query);

            var clipboard = GetClipboard();

            if (clipboard != null)
            {
                await clipboard.SetTextAsync(htmlContent);
                StatusMessage = "Book HTML copied to clipboard. Paste into a .html file to save.";
                Log.Information("Exported book {BookId} to clipboard", BookId);
            }
            else
            {
                StatusMessage = "Export failed: Clipboard not available";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting book: {ex.Message}";
            Log.Error(ex, "Failed to export book {BookId}. Error: {Error} ", BookId, ex.FullMessage());
        }
        finally
        {
            IsLoading = false;
        }
    }
}