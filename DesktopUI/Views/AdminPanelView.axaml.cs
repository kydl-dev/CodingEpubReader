using Avalonia.Controls;
using DesktopUI.ViewModels;
using ReactiveUI;

namespace DesktopUI.Views;

public partial class AdminPanelView : UserControl, IViewFor<AdminPanelViewModel>
{
    public AdminPanelView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (ViewModel is IActivatableViewModel activatable) activatable.Activator.Activate();
        };

        Unloaded += (_, _) =>
        {
            if (ViewModel is IActivatableViewModel activatable) activatable.Activator.Deactivate();
        };
    }

    public AdminPanelViewModel? ViewModel
    {
        get => DataContext as AdminPanelViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as AdminPanelViewModel;
    }
}