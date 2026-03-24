using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Grimoire.Desktop.Data;
using Grimoire.Desktop.Services;
using Grimoire.Desktop.ViewModels;
using Grimoire.Desktop.Views;
using Grimoire.Emulators;
using Grimoire.Emulators.Handlers;
using Grimoire.Shared.ApiClient;
using Grimoire.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Grimoire.Desktop;

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
            "Grimoire", "logs", "client-.log");

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
            "Grimoire", "local.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<LocalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Settings
        services.AddScoped<ISettingsService, SettingsService>();

        // API Client with resilience policies
        services.AddHttpClient<IGrimoireApi, GrimoireApiClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5038");
            client.Timeout = TimeSpan.FromMinutes(30);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // Attempt timeout must be less than half the circuit breaker sampling duration
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
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
