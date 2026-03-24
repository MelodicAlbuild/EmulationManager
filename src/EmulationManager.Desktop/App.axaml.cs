using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EmulationManager.Desktop.Data;
using EmulationManager.Desktop.Services;
using EmulationManager.Desktop.ViewModels;
using EmulationManager.Desktop.Views;
using EmulationManager.Emulators;
using EmulationManager.Emulators.Handlers;
using EmulationManager.Shared.ApiClient;
using EmulationManager.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EmulationManager.Desktop;

public class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Ensure local database exists
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            db.Database.EnsureCreated();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            // Global unhandled exception handler
            desktop.MainWindow.Closing += (_, _) => Log.CloseAndFlush();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EmulationManager", "logs", "client-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Local database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EmulationManager", "local.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<LocalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Settings
        services.AddScoped<ISettingsService, SettingsService>();

        // API Client with resilience policies
        services.AddHttpClient<IEmulationManagerApi, EmulationManagerApiClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5038");
            client.Timeout = TimeSpan.FromMinutes(30);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Retry up to 3 times with exponential backoff for transient failures
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // 5-minute total timeout for large downloads
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);

            // Per-attempt timeout of 2 minutes
            options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
        });

        // Emulator handlers
        services.AddSingleton<IEmulatorHandler, RyubingHandler>();
        services.AddSingleton<IEmulatorHandler, MelonDSHandler>();
        services.AddSingleton<IEmulatorHandler, CitraHandler>();
        services.AddSingleton<EmulatorHandlerRegistry>();

        // Core services
        services.AddSingleton<IProtocolHandler, ProtocolHandler>();
        services.AddSingleton<IEmulatorManager, EmulatorManager>();
        services.AddSingleton<IDownloadManager, DownloadManager>();
        services.AddScoped<ILaunchService, LaunchService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<GameLibraryViewModel>();
        services.AddTransient<DownloadsViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
