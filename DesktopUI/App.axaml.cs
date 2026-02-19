using System;
using System.IO;
using Application;
using Application.Interfaces;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaWebView;
using DesktopUI.ViewModels;
using DesktopUI.Views;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Exceptions;
using AvaApp = Avalonia.Application;
using DrawingColor = System.Drawing.Color;

namespace DesktopUI;

public class App : AvaApp
{
    public IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            AvaloniaWebViewBuilder.Initialize(config => { config.DefaultWebViewBackgroundColor = DrawingColor.White; });

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.Development.json", true, true)
                .Build();

            // Configure services
            var services = new ServiceCollection();

            // Add configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Add logging
            services.AddLogging(builder => builder.AddSerilog(dispose: true));

            // Add Infrastructure services (this includes DbContext, Repositories, etc.)
            services.AddInfrastructureServices(configuration);

            // Add Application services
            services.AddApplicationServices();

            // Add ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<BookLibraryViewModel>();
            services.AddTransient<AdminPanelViewModel>();
            services.AddTransient<ThemeSettingsViewModel>();

            // Build service provider
            ServiceProvider = services.BuildServiceProvider();

            // Initialize database
            try
            {
                await ServiceProvider.InitializeDatabaseAsync();
                await ServiceProvider.SeedDatabaseAsync();
                Log.Information("Database initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize database. Error: {Error}", ex.FullMessage());
            }

            // Load and apply theme
            try
            {
                var themeService = ServiceProvider.GetRequiredService<IThemeService>();
                var theme = await themeService.LoadSavedThemeAsync();

                // Apply theme to Avalonia
                ApplyThemeToAvalonia(theme);

                // Subscribe to theme changes
                themeService.ThemeChanged += (_, newTheme) => ApplyThemeToAvalonia(newTheme);

                Log.Information("Theme loaded and applied: {ThemeName}", theme.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load theme, using default. Error: {Error}", ex.FullMessage());
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainViewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception e)
        {
            Log.Error("Failed to initialize framework, error: {Error}", e.FullMessage());
        }
    }

    private void ApplyThemeToAvalonia(Theme theme)
    {
        // Set the Avalonia theme variant
        RequestedThemeVariant = theme.AvaloniaThemeVariant switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Dark
        };

        ApplyUiPalette(theme.Kind);
        Log.Information("Applied Avalonia theme variant: {Variant}", theme.AvaloniaThemeVariant);
    }

    private void ApplyUiPalette(ThemeKind kind)
    {
        var palette = kind switch
        {
            ThemeKind.Sepia => new UiPalette(
                "#F4ECD8",
                "#EBE3D0",
                "#E3D9C4",
                "#D4C4A8",
                "#3B2A1A",
                "#5A422D",
                "#7A654C",
                "#E5D9C2",
                "#3B2A1A",
                "#EFE4CD",
                "#CCB691"),
            ThemeKind.Dark => new UiPalette(
                "#121316",
                "#1B1D22",
                "#22252B",
                "#333843",
                "#EAEAF0",
                "#BEC3D1",
                "#8F95A6",
                "#2A2E37",
                "#EAEAF0",
                "#181A20",
                "#353B47"),
            _ => new UiPalette(
                "#FFFFFF",
                "#F5F5F5",
                "#EFEFEF",
                "#DDDDDD",
                "#1A1A1A",
                "#444444",
                "#6A6A6A",
                "#E6E6E6",
                "#1A1A1A",
                "#FAFAFA",
                "#D4D4D4")
        };

        SetBrushResource("AppBackgroundBrush", palette.Background);
        SetBrushResource("AppSurfaceBrush", palette.Surface);
        SetBrushResource("AppSurfaceAltBrush", palette.SurfaceAlt);
        SetBrushResource("AppBorderBrush", palette.Border);
        SetBrushResource("AppTextBrush", palette.Text);
        SetBrushResource("AppTextMutedBrush", palette.TextMuted);
        SetBrushResource("AppTextLowBrush", palette.TextLow);
        SetBrushResource("AppButtonBackgroundBrush", palette.ButtonBackground);
        SetBrushResource("AppButtonForegroundBrush", palette.ButtonForeground);
        SetBrushResource("AppCardBackgroundBrush", palette.CardBackground);
        SetBrushResource("AppCardBorderBrush", palette.CardBorder);
    }

    private void SetBrushResource(string key, string hexColor)
    {
        var color = Color.Parse(hexColor);
        Resources[key] = new SolidColorBrush(color);
    }

    private sealed record UiPalette(
        string Background,
        string Surface,
        string SurfaceAlt,
        string Border,
        string Text,
        string TextMuted,
        string TextLow,
        string ButtonBackground,
        string ButtonForeground,
        string CardBackground,
        string CardBorder);
}