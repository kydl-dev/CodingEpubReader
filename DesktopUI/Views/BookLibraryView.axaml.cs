using System;
using System.ComponentModel;
using Avalonia.Controls;
using DesktopUI.ViewModels;
using ReactiveUI;
using WebViewCore;

namespace DesktopUI.Views;

public partial class BookLibraryView : UserControl, IViewFor<BookLibraryViewModel>
{
    private BookLibraryViewModel? _subscribedViewModel;

    public BookLibraryView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Manually activate when control loads
        Loaded += (sender, args) =>
        {
            if (ViewModel is IActivatableViewModel activatable) activatable.Activator.Activate();
        };

        // Deactivate when unloaded
        Unloaded += (sender, args) =>
        {
            if (ViewModel is IActivatableViewModel activatable) activatable.Activator.Deactivate();
        };
    }

    public BookLibraryViewModel? ViewModel
    {
        get => DataContext as BookLibraryViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as BookLibraryViewModel;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel != null) _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _subscribedViewModel = ViewModel;
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
            RenderDetailsHtml();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BookLibraryViewModel.SelectedBookDetails) ||
            e.PropertyName == nameof(BookLibraryViewModel.IsBookDetailsVisible))
            RenderDetailsHtml();
    }

    private void RenderDetailsHtml()
    {
        if (ViewModel == null || !ViewModel.IsBookDetailsVisible) return;

        var html = ViewModel.SelectedBookDetails?.DescriptionHtmlDocument;
        if (string.IsNullOrWhiteSpace(html)) return;

        if (DetailsDescriptionWebView is IWebViewControl typedWebView) typedWebView.NavigateToString(html);
    }
}