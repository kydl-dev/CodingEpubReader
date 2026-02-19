using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.UseCases.Books.OpenBook;
using Application.UseCases.ReadingProgress.GetReadingProgress;
using Domain.Repositories;
using MediatR;
using ReactiveUI;
using Serilog;
using Shared.Exceptions;
using Unit = System.Reactive.Unit;

namespace DesktopUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly AdminPanelViewModel _adminPanelViewModel;
    private readonly IBookContentService _bookContentService;
    private readonly IMediator _mediator;
    private readonly ISavedCssStyleRepository _savedCssStyleRepository;
    private readonly IThemeService _themeService;
    private readonly ThemeSettingsViewModel _themeSettingsViewModel;
    private ViewModelBase _currentView;
    private bool _isMenuOpen;

    public MainWindowViewModel(
        BookLibraryViewModel bookLibraryViewModel,
        AdminPanelViewModel adminPanelViewModel,
        ThemeSettingsViewModel themeSettingsViewModel,
        IMediator mediator,
        IBookContentService bookContentService,
        IThemeService themeService,
        ISavedCssStyleRepository savedCssStyleRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _bookContentService = bookContentService ?? throw new ArgumentNullException(nameof(bookContentService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _savedCssStyleRepository =
            savedCssStyleRepository ?? throw new ArgumentNullException(nameof(savedCssStyleRepository));
        _adminPanelViewModel = adminPanelViewModel ?? throw new ArgumentNullException(nameof(adminPanelViewModel));
        _themeSettingsViewModel =
            themeSettingsViewModel ?? throw new ArgumentNullException(nameof(themeSettingsViewModel));
        BookLibrary = bookLibraryViewModel ?? throw new ArgumentNullException(nameof(bookLibraryViewModel));
        _currentView = BookLibrary;

        // CreateFromTask so OpenBookCommand can await the mediator call (MarkAsOpened write).
        OpenBookCommand = ReactiveCommand.CreateFromTask<BookViewModel>(
            OpenBookAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        OpenBookCommand.ThrownExceptions
            .Subscribe(ex => Log.Error(ex, "Failed to open book. Error: {Error}", ex.FullMessage()));

        OpenBookByIdCommand = ReactiveCommand.CreateFromTask<Guid?>(
            OpenBookByIdAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        OpenBookByIdCommand.ThrownExceptions
            .Subscribe(ex => Log.Error(ex, "Failed to open book by id. Error: {Error}", ex.FullMessage()));

        ToggleMenuCommand = ReactiveCommand.Create(ToggleMenu);
        ShowLibraryCommand = ReactiveCommand.Create(ShowLibrary);
        ShowRecentlyReadCommand = ReactiveCommand.CreateFromTask(ShowRecentlyReadAsync);
        ShowAdminCommand = ReactiveCommand.Create(ShowAdmin);
        ShowThemesCommand = ReactiveCommand.Create(ShowThemes);
    }

    public string Greeting => "Epub Reader";

    private BookLibraryViewModel BookLibrary { get; }

    public ViewModelBase CurrentView
    {
        get => _currentView;
        private set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public bool IsMenuOpen
    {
        get => _isMenuOpen;
        private set => this.RaiseAndSetIfChanged(ref _isMenuOpen, value);
    }

    public ReactiveCommand<BookViewModel, Unit> OpenBookCommand { get; }
    public ReactiveCommand<Guid?, Unit> OpenBookByIdCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleMenuCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowLibraryCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowRecentlyReadCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAdminCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowThemesCommand { get; }

    private async Task OpenBookAsync(BookViewModel book)
    {
        // OpenBookCommand calls book.MarkAsOpened() and persists LastOpenedDate.
        var bookDto = await _mediator.Send(new OpenBookCommand(book.Id));
        var savedPosition = await _mediator.Send(new GetReadingProgressQuery(book.Id));

        var preferredChapterId = savedPosition?.ChapterId
                                 ?? bookDto.Chapters
                                     .OrderBy(c => c.Order)
                                     .Select(c => c.Id)
                                     .FirstOrDefault();

        var readerViewModel = new EpubReaderViewModel(
            _mediator,
            _bookContentService,
            _themeService,
            _savedCssStyleRepository,
            bookDto.Id,
            bookDto.Title,
            preferredChapterId,
            savedPosition?.Progress ?? 0.0);

        CurrentView = readerViewModel;
    }

    private async Task OpenBookByIdAsync(Guid? bookId)
    {
        if (!bookId.HasValue || bookId.Value == Guid.Empty) return;

        var book = BookLibrary.Books.FirstOrDefault(b => b.Id == bookId.Value);

        if (book == null) return;

        await OpenBookAsync(book);
    }

    private void ShowLibrary()
    {
        BookLibrary.ActivateLibraryMode();
        CurrentView = BookLibrary;
        IsMenuOpen = false;
    }

    private async Task ShowRecentlyReadAsync()
    {
        await BookLibrary.ActivateRecentlyReadModeAsync();
        CurrentView = BookLibrary;
        IsMenuOpen = false;
    }

    private void ShowAdmin()
    {
        CurrentView = _adminPanelViewModel;
        IsMenuOpen = false;
    }

    private void ShowThemes()
    {
        CurrentView = _themeSettingsViewModel;
        IsMenuOpen = false;
    }

    private void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
    }
}