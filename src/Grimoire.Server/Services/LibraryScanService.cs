using Grimoire.Server.Data;
using Grimoire.Server.Entities;
using Grimoire.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Grimoire.Server.Services;

/// <summary>
/// Background service that scans configured storage directories
/// and auto-imports games into the database.
/// </summary>
public class LibraryScanService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LibraryScanService> _logger;
    private readonly StorageOptions _storage;

    private static readonly Dictionary<string, PlatformType> ExtensionToPlatform = new(StringComparer.OrdinalIgnoreCase)
    {
        [".nsp"] = PlatformType.NintendoSwitch,
        [".xci"] = PlatformType.NintendoSwitch,
        [".nca"] = PlatformType.NintendoSwitch,
        [".nds"] = PlatformType.NintendoDS,
        [".dsi"] = PlatformType.NintendoDS,
        [".3ds"] = PlatformType.Nintendo3DS,
        [".cci"] = PlatformType.Nintendo3DS,
        [".cxi"] = PlatformType.Nintendo3DS,
        [".cia"] = PlatformType.Nintendo3DS,
    };

    public LibraryScanService(IServiceProvider services, ILogger<LibraryScanService> logger,
        IOptions<StorageOptions> storage)
    {
        _services = services;
        _logger = logger;
        _storage = storage.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run initial scan after a short delay to let the app start up
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await ScanAsync(stoppingToken);
    }

    public async Task ScanAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_storage.GamesBasePath))
        {
            _logger.LogInformation("Games base path not configured, skipping library scan");
            return;
        }

        if (!Directory.Exists(_storage.GamesBasePath))
        {
            _logger.LogWarning("Games base path does not exist: {Path}", _storage.GamesBasePath);
            return;
        }

        _logger.LogInformation("Starting library scan of {Path}", _storage.GamesBasePath);

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrimoireDbContext>();

        var existingPaths = await db.Games.Select(g => g.FilePath).ToHashSetAsync(ct);
        var newGames = new List<GameEntity>();

        foreach (var file in Directory.EnumerateFiles(_storage.GamesBasePath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!ExtensionToPlatform.TryGetValue(ext, out var platform))
                continue;

            var relativePath = Path.GetRelativePath(_storage.GamesBasePath, file);
            if (existingPaths.Contains(relativePath))
                continue;

            var fileInfo = new FileInfo(file);
            var title = Path.GetFileNameWithoutExtension(file)
                .Replace('_', ' ')
                .Replace('-', ' ');

            newGames.Add(new GameEntity
            {
                Title = title,
                Platform = platform,
                FilePath = relativePath,
                FileSize = fileInfo.Length
            });
        }

        if (newGames.Count > 0)
        {
            db.Games.AddRange(newGames);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Library scan complete: imported {Count} new games", newGames.Count);
        }
        else
        {
            _logger.LogInformation("Library scan complete: no new games found");
        }
    }
}
