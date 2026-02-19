using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Enums;
using ReactiveUI;

namespace DesktopUI.ViewModels;

public sealed class ThemeSettingsViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private string _selectedTheme;

    public ThemeSettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        ThemeOptions = new ObservableCollection<string>(["Dark", "Light", "Sepia"]);
        _selectedTheme = _themeService.CurrentTheme.Kind.ToString();
        ApplyThemeCommand = ReactiveCommand.CreateFromTask(ApplyThemeAsync);
    }

    public ObservableCollection<string> ThemeOptions { get; }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedTheme, value);
    }

    public ReactiveCommand<Unit, Unit> ApplyThemeCommand { get; }

    private async Task ApplyThemeAsync()
    {
        if (!Enum.TryParse<ThemeKind>(SelectedTheme, true, out var kind)) return;

        await _themeService.SetThemeAsync(kind);
    }
}