using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.UseCases.Books.DeleteBook;
using Application.UseCases.Books.GetAllBooks;
using Application.UseCases.Books.GetBookDetails;
using Avalonia.Media.Imaging;
using Domain.Repositories;
using MediatR;
using ReactiveUI;
using Serilog;
using Shared.BackgroundWorkers.Configuration;
using Shared.Exceptions;
using Unit = System.Reactive.Unit;

namespace DesktopUI.ViewModels;

public class BookLibraryViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly ILibraryService _libraryService;
    private readonly IMediator _mediator;
    private readonly IReadingHistoryRepository _readingHistoryRepository;
    private readonly ILibraryScanConfiguration _scanConfiguration;
    private ObservableCollection<BookViewModel> _books;
    private bool _isBookDetailsVisible;
    private bool _isDeleteConfirmVisible;
    private bool _isRecentlyReadMode;
    private string _libraryDescription;
    private string _libraryName;
    private BookViewModel? _pendingDeleteBook;
    private ObservableCollection<ReadingHistoryItemViewModel> _recentlyRead;
    private BookDetailsViewModel? _selectedBookDetails;
    private string _statusMessage;
    private int _totalBooks;

    public BookLibraryViewModel(
        IMediator mediator,
        ILibraryService libraryService,
        ILibraryScanConfiguration scanConfiguration,
        IReadingHistoryRepository readingHistoryRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _scanConfiguration = scanConfiguration ?? throw new ArgumentNullException(nameof(scanConfiguration));
        _readingHistoryRepository = readingHistoryRepository ??
                                    throw new ArgumentNullException(nameof(readingHistoryRepository));
        _books = [];
        _recentlyRead = [];
        _statusMessage = "Ready to load books...";
        _libraryName = "My Library";
        _libraryDescription = string.Empty;
        _totalBooks = 0;

        LoadBooksCommand = ReactiveCommand.CreateFromTask(
            LoadBooksAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        RefreshLibraryCommand = ReactiveCommand.CreateFromTask(
            RefreshLibraryAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        DeleteBookCommand = ReactiveCommand.CreateFromTask<BookViewModel>(
            DeleteBookAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        PromptDeleteBookCommand = ReactiveCommand.Create<BookViewModel>(PromptDeleteBook);
        ConfirmDeleteBookCommand = ReactiveCommand.CreateFromTask(
            ConfirmDeleteBookAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        CancelDeleteBookCommand = ReactiveCommand.Create(CancelDeleteBook);

        ShowBookDetailsCommand = ReactiveCommand.CreateFromTask<BookViewModel>(
            ShowBookDetailsAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        CloseBookDetailsCommand = ReactiveCommand.Create(CloseBookDetails);

        Activator = new ViewModelActivator();

        this.WhenActivated(disposables =>
        {
            LoadBooksCommand.Execute()
                .Subscribe()
                .DisposeWith(disposables);

            LoadBooksCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error loading books: {ex.Message}";
                    Log.Error(ex, "Failed to load books. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            RefreshLibraryCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error refreshing library: {ex.Message}";
                    Log.Error(ex, "Failed to refresh library. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            DeleteBookCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error deleting book: {ex.Message}";
                    Log.Error(ex, "Failed to delete book. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            ConfirmDeleteBookCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error deleting book: {ex.Message}";
                    Log.Error(ex, "Failed to confirm-delete book. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            ShowBookDetailsCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Error loading book details: {ex.Message}";
                    Log.Error(ex, "Failed to load book details. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);
        });
    }

    public BookLibraryViewModel()
    {
        // Design-time constructor for XAML previewers.
        _mediator = null!;
        _libraryService = null!;
        _scanConfiguration = null!;
        _readingHistoryRepository = null!;

        _books = [];
        _recentlyRead = [];
        _statusMessage = "Design mode";
        _libraryName = "My Library";
        _libraryDescription = string.Empty;
        _totalBooks = 0;

        Activator = new ViewModelActivator();

        LoadBooksCommand = ReactiveCommand.Create(() => { });
        RefreshLibraryCommand = ReactiveCommand.Create(() => { });
        DeleteBookCommand = ReactiveCommand.Create<BookViewModel>(_ => { });
        PromptDeleteBookCommand = ReactiveCommand.Create<BookViewModel>(_ => { });
        ConfirmDeleteBookCommand = ReactiveCommand.Create(() => { });
        CancelDeleteBookCommand = ReactiveCommand.Create(() => { });
        ShowBookDetailsCommand = ReactiveCommand.Create<BookViewModel>(_ => { });
        CloseBookDetailsCommand = ReactiveCommand.Create(() => { });
    }

    public ObservableCollection<BookViewModel> Books
    {
        get => _books;
        set => this.RaiseAndSetIfChanged(ref _books, value);
    }

    public ObservableCollection<ReadingHistoryItemViewModel> RecentlyRead
    {
        get => _recentlyRead;
        set => this.RaiseAndSetIfChanged(ref _recentlyRead, value);
    }

    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string LibraryName
    {
        get => _libraryName;
        set => this.RaiseAndSetIfChanged(ref _libraryName, value);
    }

    public string LibraryDescription
    {
        get => _libraryDescription;
        set => this.RaiseAndSetIfChanged(ref _libraryDescription, value);
    }

    public int TotalBooks
    {
        get => _totalBooks;
        set => this.RaiseAndSetIfChanged(ref _totalBooks, value);
    }

    public DateTime? LibraryCreatedAt
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public DateTime? LibraryLastUpdatedAt
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public BookDetailsViewModel? SelectedBookDetails
    {
        get => _selectedBookDetails;
        set => this.RaiseAndSetIfChanged(ref _selectedBookDetails, value);
    }

    public bool IsBookDetailsVisible
    {
        get => _isBookDetailsVisible;
        set => this.RaiseAndSetIfChanged(ref _isBookDetailsVisible, value);
    }

    public BookViewModel? PendingDeleteBook
    {
        get => _pendingDeleteBook;
        private set => this.RaiseAndSetIfChanged(ref _pendingDeleteBook, value);
    }

    public bool IsDeleteConfirmVisible
    {
        get => _isDeleteConfirmVisible;
        private set => this.RaiseAndSetIfChanged(ref _isDeleteConfirmVisible, value);
    }

    public bool IsRecentlyReadMode
    {
        get => _isRecentlyReadMode;
        private set => this.RaiseAndSetIfChanged(ref _isRecentlyReadMode, value);
    }

    public bool IsLibraryMode => !IsRecentlyReadMode;

    public ReactiveCommand<Unit, Unit> LoadBooksCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshLibraryCommand { get; }
    public ReactiveCommand<BookViewModel, Unit> DeleteBookCommand { get; }
    public ReactiveCommand<BookViewModel, Unit> PromptDeleteBookCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmDeleteBookCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelDeleteBookCommand { get; }
    public ReactiveCommand<BookViewModel, Unit> ShowBookDetailsCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseBookDetailsCommand { get; }

    public ViewModelActivator Activator { get; }

    private async Task LoadBooksAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading books from library...";

            // GetAllBooksQuery enriches each book with reading progress and sorts by LastOpened.
            var summaries = await _mediator.Send(new GetAllBooksQuery());

            Books.Clear();
            foreach (var dto in summaries)
                Books.Add(new BookViewModel
                {
                    Id = dto.Id,
                    Title = dto.Title,
                    PrimaryAuthor = dto.PrimaryAuthor,
                    FilePath = dto.FilePath,
                    CoverImagePath = dto.CoverImagePath,
                    CoverPreviewImage = BuildCoverPreviewImage(dto.FilePath, dto.CoverImagePath),
                    Language = dto.Language,
                    AddedDate = dto.AddedDate,
                    LastOpenedDate = dto.LastOpenedDate,
                    OverallProgress = dto.OverallProgress
                });

            TotalBooks = Books.Count;
            await LoadRecentHistoryAsync();
            StatusMessage = $"Loaded {Books.Count} book(s)";
            Log.Information("Loaded {BookCount} books via GetAllBooksQuery", Books.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading books: {ex.Message}";
            Log.Error(ex, "Failed to load books. Error: {Error}", ex.FullMessage());
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshLibraryAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Scanning watched folders for new books...";

            var watchedFolders = _scanConfiguration.GetWatchedFolders();
            var totalImported = 0;

            foreach (var folder in watchedFolders)
                try
                {
                    StatusMessage = $"Scanning folder: {folder}";
                    if (folder == null) continue;
                    var imported = await _libraryService.ImportFolderAsync(folder);
                    totalImported += imported;
                    Log.Information("Imported {Count} books from {Folder}", imported, folder);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to scan folder {Folder}. Error: {Error}", folder, ex.FullMessage());
                    StatusMessage = $"Error scanning {folder}: {ex.Message}";
                }

            StatusMessage = $"Scan complete. Imported {totalImported} new book(s). Reloading library...";
            await LoadBooksAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scanning library: {ex.Message}";
            Log.Error(ex, "Failed to scan library. Error: {Error}", ex.FullMessage());
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteBookAsync(BookViewModel? book)
    {
        if (book == null) return;

        IsLoading = true;
        try
        {
            StatusMessage = $"Deleting '{book.Title}'...";
            await _mediator.Send(new DeleteBookCommand(book.Id));

            var existing = Books.FirstOrDefault(b => b.Id == book.Id);
            if (existing != null) Books.Remove(existing);

            TotalBooks = Books.Count;
            await LoadRecentHistoryAsync();
            StatusMessage = $"Deleted '{book.Title}'.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PromptDeleteBook(BookViewModel? book)
    {
        if (book == null) return;

        PendingDeleteBook = book;
        IsDeleteConfirmVisible = true;
    }

    private async Task ConfirmDeleteBookAsync()
    {
        if (PendingDeleteBook == null)
        {
            IsDeleteConfirmVisible = false;
            return;
        }

        var book = PendingDeleteBook;
        IsDeleteConfirmVisible = false;
        PendingDeleteBook = null;

        await DeleteBookAsync(book);
    }

    private void CancelDeleteBook()
    {
        IsDeleteConfirmVisible = false;
        PendingDeleteBook = null;
    }

    private async Task ShowBookDetailsAsync(BookViewModel? book)
    {
        if (book == null) return;

        var details = await _mediator.Send(new GetBookDetailsQuery(book.Id));
        SelectedBookDetails = new BookDetailsViewModel
        {
            Title = details.Title,
            Authors = details.Authors.Count > 0 ? string.Join(", ", details.Authors) : "Unknown",
            Language = string.IsNullOrWhiteSpace(details.Language) ? "Unknown" : details.Language,
            Publisher = string.IsNullOrWhiteSpace(details.Metadata.Publisher) ? "Unknown" : details.Metadata.Publisher,
            Isbn = string.IsNullOrWhiteSpace(details.Metadata.Isbn) ? "N/A" : details.Metadata.Isbn,
            PublishedDate = details.Metadata.PublishedDate?.ToString("yyyy-MM-dd") ?? "Unknown",
            Subject = string.IsNullOrWhiteSpace(details.Metadata.Subject) ? "N/A" : details.Metadata.Subject,
            Rights = string.IsNullOrWhiteSpace(details.Metadata.Rights) ? "N/A" : details.Metadata.Rights,
            EpubVersion = string.IsNullOrWhiteSpace(details.Metadata.EpubVersion)
                ? "Unknown"
                : details.Metadata.EpubVersion,
            Description = string.IsNullOrWhiteSpace(details.Metadata.Description)
                ? "No description available."
                : details.Metadata.Description,
            DescriptionHtmlDocument = BuildHtmlDocument(details.Metadata.Description)
        };

        IsBookDetailsVisible = true;
    }

    private void CloseBookDetails()
    {
        IsBookDetailsVisible = false;
    }

    private async Task LoadRecentHistoryAsync()
    {
        var recent = await _readingHistoryRepository.GetRecentAsync();

        RecentlyRead.Clear();
        foreach (var entry in recent)
            RecentlyRead.Add(new ReadingHistoryItemViewModel
            {
                BookId = entry.BookId?.Value,
                Title = string.IsNullOrWhiteSpace(entry.BookTitle) ? "Unknown Book" : entry.BookTitle,
                Author = string.IsNullOrWhiteSpace(entry.BookAuthor) ? "Unknown Author" : entry.BookAuthor,
                LastReadAt = entry.LastReadAt,
                TotalReadingTime = entry.TotalReadingTime,
                TotalSessions = entry.TotalSessions
            });
    }

    public async Task ActivateRecentlyReadModeAsync()
    {
        IsRecentlyReadMode = true;
        this.RaisePropertyChanged(nameof(IsLibraryMode));
        await LoadRecentHistoryAsync();
        StatusMessage = RecentlyRead.Count == 0 ? "No recent reading history found." : "Recently read books loaded.";
    }

    public void ActivateLibraryMode()
    {
        IsRecentlyReadMode = false;
        this.RaisePropertyChanged(nameof(IsLibraryMode));
    }

    private static string BuildHtmlDocument(string? rawHtml)
    {
        var body = string.IsNullOrWhiteSpace(rawHtml) ? "<p>No description available.</p>" : rawHtml;
        return $$"""
                 <html>
                   <head>
                     <meta charset="utf-8" />
                     <style>
                       body { font-family: Segoe UI, Arial, sans-serif; font-size: 14px; line-height: 1.5; margin: 0; padding: 6px; color: #d7dbe6; background: transparent; }
                       a { color: #7ab8ff; }
                       p { margin: 0 0 8px 0; }
                     </style>
                   </head>
                   <body>{{body}}</body>
                 </html>
                 """;
    }

    private static Bitmap? BuildCoverPreviewImage(string bookFilePath, string? coverImagePath)
    {
        if (string.IsNullOrWhiteSpace(bookFilePath) || !File.Exists(bookFilePath)) return null;

        var normalizedCoverPath = NormalizeEpubPath(coverImagePath);
        var targetFileName = Path.GetFileName(normalizedCoverPath);

        try
        {
            using var archive = ZipFile.OpenRead(bookFilePath);
            var entries = archive.Entries
                .Where(e => e.Length > 0)
                .Select(e => new ArchiveEntryView(
                    e,
                    NormalizeEpubPath(e.FullName),
                    Path.GetFileName(e.FullName)))
                .ToList();

            var entry = ResolveCoverEntry(entries, normalizedCoverPath, targetFileName);

            if (entry == null)
            {
                Log.Debug("No cover entry resolved for book file {BookFilePath}. Stored cover path: {CoverPath}",
                    bookFilePath, coverImagePath);
                return null;
            }

            using var entryStream = entry.Open();
            using var memory = new MemoryStream();
            entryStream.CopyTo(memory);
            var bytes = memory.ToArray();
            var persistentStream = new MemoryStream(bytes, false);
            return new Bitmap(persistentStream);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to decode cover for {BookFilePath}. Stored cover path: {CoverPath}", bookFilePath,
                coverImagePath);
            return null;
        }
    }

    private static ZipArchiveEntry? ResolveCoverEntry(
        IEnumerable<ArchiveEntryView> entries,
        string normalizedCoverPath,
        string? targetFileName)
    {
        var entriesList = entries.ToList();

        if (!string.IsNullOrWhiteSpace(normalizedCoverPath))
        {
            var exact = entriesList.FirstOrDefault(e =>
                string.Equals(e.NormalizedPath, normalizedCoverPath, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact.Entry;

            var suffix = entriesList.FirstOrDefault(e =>
                e.NormalizedPath.EndsWith("/" + normalizedCoverPath, StringComparison.OrdinalIgnoreCase));
            if (suffix != null) return suffix.Entry;
        }

        if (!string.IsNullOrWhiteSpace(targetFileName))
        {
            var byFileName = entriesList.FirstOrDefault(e =>
                string.Equals(e.FileName, targetFileName, StringComparison.OrdinalIgnoreCase));
            if (byFileName != null) return byFileName.Entry;
        }

        var namedCover = entriesList.FirstOrDefault(e =>
        {
            var path = e.NormalizedPath.ToLowerInvariant();
            return path.Contains("cover.") || path.Contains("cover_") || path.Contains("cover-");
        });
        if (namedCover != null) return namedCover.Entry;

        return entriesList
            .Select(e => e.Entry)
            .FirstOrDefault(e => IsSupportedImageExtension(e.FullName));
    }

    private static string NormalizeEpubPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        var value = WebUtility.UrlDecode(path).Replace('\\', '/').Trim();
        value = value.TrimStart('/');
        var queryIndex = value.IndexOfAny(['?', '#']);
        if (queryIndex >= 0) value = value[..queryIndex];

        if (value.StartsWith("./", StringComparison.Ordinal)) value = value[2..];

        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
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

    private static bool IsSupportedImageExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => true,
            _ => false
        };
    }

    private sealed record ArchiveEntryView(
        ZipArchiveEntry Entry,
        string NormalizedPath,
        string FileName);
}