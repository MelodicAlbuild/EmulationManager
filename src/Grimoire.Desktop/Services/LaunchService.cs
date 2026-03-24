using System.Diagnostics;
using Grimoire.Desktop.Data;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;
using Grimoire.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Grimoire.Desktop.Services;

public enum LaunchStep
{
    ResolvingGame,
    CheckingEmulator,
    DownloadingEmulator,
    InstallingEmulator,
    DownloadingGame,
    InstallingDlc,
    InstallingUpdates,
    ValidatingRequirements,
    Launching,
    Complete,
    Failed
}

public record LaunchProgress(LaunchStep Step, string Message, double? ProgressPercent = null);

public interface ILaunchService
{
    Task LaunchGameAsync(int gameId, IProgress<LaunchProgress> progress, CancellationToken ct = default);
}

public class LaunchService : ILaunchService
{
    private readonly IGrimoireApi _api;
    private readonly IEmulatorManager _emulatorManager;
    private readonly IDownloadManager _downloadManager;
    private readonly ISettingsService _settings;
    private readonly LocalDbContext _db;

    public LaunchService(
        IGrimoireApi api,
        IEmulatorManager emulatorManager,
        IDownloadManager downloadManager,
        ISettingsService settings,
        LocalDbContext db)
    {
        _api = api;
        _emulatorManager = emulatorManager;
        _downloadManager = downloadManager;
        _settings = settings;
        _db = db;
    }

    public async Task LaunchGameAsync(int gameId, IProgress<LaunchProgress> progress, CancellationToken ct = default)
    {
        try
        {
            // 1. Resolve game details
            progress.Report(new LaunchProgress(LaunchStep.ResolvingGame, "Fetching game details..."));
            var game = await _api.GetGameDetailAsync(gameId, ct);
            if (game is null)
                throw new InvalidOperationException($"Game with ID {gameId} not found on server.");

            var handler = _emulatorManager.GetHandler(game.Platform);
            if (handler is null)
                throw new NotSupportedException($"No emulator handler for platform: {game.Platform}");

            // 2. Check emulator
            progress.Report(new LaunchProgress(LaunchStep.CheckingEmulator, $"Checking {handler.EmulatorName}..."));
            var emulatorPath = await handler.FindInstalledPathAsync(ct);

            if (emulatorPath is null)
            {
                progress.Report(new LaunchProgress(LaunchStep.DownloadingEmulator,
                    $"{handler.EmulatorName} not found. Automatic installation not yet implemented."));
                throw new InvalidOperationException(
                    $"{handler.EmulatorName} is not installed. Please install it manually and try again.");
            }

            // 3. Check if game is downloaded locally
            progress.Report(new LaunchProgress(LaunchStep.DownloadingGame, "Checking local game files..."));
            var installDir = await _settings.GetInstallDirectoryAsync();
            var localGame = await _db.DownloadedGames
                .FirstOrDefaultAsync(g => g.ServerGameId == gameId, ct);

            string gamePath;
            if (localGame is not null && File.Exists(localGame.LocalPath))
            {
                gamePath = localGame.LocalPath;
            }
            else
            {
                // Download the game
                progress.Report(new LaunchProgress(LaunchStep.DownloadingGame, $"Downloading {game.Title}..."));
                var gameDir = Path.Combine(installDir, "games", game.Platform.ToString().ToLower());
                var ext = handler.SupportedFileExtensions.FirstOrDefault() ?? ".bin";
                gamePath = Path.Combine(gameDir, SanitizeFileName(game.Title) + ext);

                var download = await _downloadManager.EnqueueAsync(
                    game.Title, DownloadableType.Game, gameId, gamePath);

                // Wait for download to complete
                var tcs = new TaskCompletionSource();
                void OnComplete(DownloadItem item)
                {
                    if (item.Id == download.Id) tcs.TrySetResult();
                }
                _downloadManager.DownloadCompleted += OnComplete;
                try { await tcs.Task.WaitAsync(ct); }
                finally { _downloadManager.DownloadCompleted -= OnComplete; }

                // Track in local DB
                _db.DownloadedGames.Add(new DownloadedGame
                {
                    ServerGameId = gameId,
                    Title = game.Title,
                    Platform = game.Platform,
                    LocalPath = gamePath,
                    FileSize = game.FileSize
                });
                await _db.SaveChangesAsync(ct);
            }

            // 4. Validate requirements
            progress.Report(new LaunchProgress(LaunchStep.ValidatingRequirements, "Validating emulator requirements..."));
            try
            {
                await handler.ValidateRequirementsAsync(emulatorPath, ct);
            }
            catch (FileNotFoundException ex)
            {
                progress.Report(new LaunchProgress(LaunchStep.Failed, $"Missing requirement: {ex.Message}"));
                throw;
            }

            // 5. Launch
            progress.Report(new LaunchProgress(LaunchStep.Launching, $"Launching {game.Title}..."));
            var startInfo = handler.BuildLaunchArgs(emulatorPath, gamePath, new LaunchOptions());
            Process.Start(startInfo);

            progress.Report(new LaunchProgress(LaunchStep.Complete, $"{game.Title} launched!"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            progress.Report(new LaunchProgress(LaunchStep.Failed, ex.Message));
            throw;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
