using Avalonia.Controls;
using DesktopUI.ViewModels;
using ReactiveUI;

namespace DesktopUI.Views;

public partial class BookLibraryView : UserControl, IViewFor<BookLibraryViewModel>
{
    public BookLibraryView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (ViewModel is IActivatableViewModel activatable)
                activatable.Activator.Activate();
        };

        Unloaded += (_, _) =>
        {
            if (ViewModel is IActivatableViewModel activatable)
                activatable.Activator.Deactivate();
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
}
