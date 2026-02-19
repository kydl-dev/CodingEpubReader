using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.WebView.Desktop;
using ReactiveUI.Avalonia;
using Serilog;
using Serilog.Events;
using Shared.Exceptions;

namespace DesktopUI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ConfigureWebViewRuntimeEnvironment();

        // Configure Serilog BEFORE anything else
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(GetAppDataPath(), "Logs", "epubreader-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting EpubReader application");

            AppDomain.CurrentDomain.UnhandledException += (_, unhandledExceptionEventArgs) =>
            {
                if (unhandledExceptionEventArgs.ExceptionObject is Exception ex)
                    Log.Fatal(ex, "Unhandled exception: {Message}", ex.FullMessage());

                Log.CloseAndFlush();
            };

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly: {Message}", ex.FullMessage());
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI()
            .UseDesktopWebView()
            .WithInterFont();
    }

    private static string GetAppDataPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EpubReader");
    }

    private static void ConfigureWebViewRuntimeEnvironment()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        // Workaround for black WebView surfaces and reduce Chromium shutdown noise in console.
        var existingArgs = Environment.GetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS");
        var requiredArgs = "--disable-gpu --disable-gpu-compositing --disable-logging --log-level=3";
        var combinedArgs = string.IsNullOrWhiteSpace(existingArgs)
            ? requiredArgs
            : $"{existingArgs} {requiredArgs}";

        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", combinedArgs);
    }
}