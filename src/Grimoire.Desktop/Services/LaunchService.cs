using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Grimoire.Desktop.Data;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;
using Grimoire.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    private readonly IServiceProvider _services;

    public LaunchService(
        IGrimoireApi api,
        IEmulatorManager emulatorManager,
        IDownloadManager downloadManager,
        ISettingsService settings,
        IServiceProvider services)
    {
        _api = api;
        _emulatorManager = emulatorManager;
        _downloadManager = downloadManager;
        _settings = settings;
        _services = services;
    }

    private LocalDbContext CreateDb() => _services.GetRequiredService<LocalDbContext>();

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

            // 2. Check emulator — install if missing
            progress.Report(new LaunchProgress(LaunchStep.CheckingEmulator, $"Checking {handler.EmulatorName}..."));
            var emulatorPath = await handler.FindInstalledPathAsync(ct);

            if (emulatorPath is null)
            {
                emulatorPath = await DownloadAndInstallEmulatorAsync(handler, game.Platform, progress, ct);
            }

            // 3. Check if game is downloaded locally
            progress.Report(new LaunchProgress(LaunchStep.DownloadingGame, "Checking local game files..."));
            var installDir = await _settings.GetInstallDirectoryAsync();
            DownloadedGame? localGame;
            using (var db = CreateDb())
            {
                localGame = await db.DownloadedGames
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.ServerGameId == gameId, ct);
            }

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

                // Wait for download to complete, relaying progress
                var tcs = new TaskCompletionSource();
                void OnComplete(DownloadItem item)
                {
                    if (item.Id == download.Id) tcs.TrySetResult();
                }
                void OnProgress(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    if (e.PropertyName is nameof(DownloadItem.BytesDownloaded) or nameof(DownloadItem.ProgressPercent))
                    {
                        progress.Report(new LaunchProgress(
                            LaunchStep.DownloadingGame,
                            $"Downloading {game.Title}... {download.ProgressPercent:F1}% ({download.ProgressText})",
                            download.ProgressPercent));
                    }
                }
                download.PropertyChanged += OnProgress;
                _downloadManager.DownloadCompleted += OnComplete;
                try { await tcs.Task.WaitAsync(ct); }
                finally
                {
                    _downloadManager.DownloadCompleted -= OnComplete;
                    download.PropertyChanged -= OnProgress;
                }

                // Track in local DB
                using (var db = CreateDb())
                {
                    db.DownloadedGames.Add(new DownloadedGame
                    {
                        ServerGameId = gameId,
                        Title = game.Title,
                        Platform = game.Platform,
                        LocalPath = gamePath,
                        FileSize = game.FileSize
                    });
                    await db.SaveChangesAsync(ct);
                }
            }

            // 4. Validate requirements — auto-download if missing
            progress.Report(new LaunchProgress(LaunchStep.ValidatingRequirements, "Validating emulator requirements..."));
            try
            {
                await handler.ValidateRequirementsAsync(emulatorPath, ct);
            }
            catch (FileNotFoundException)
            {
                // Requirements missing — try to fetch from server
                progress.Report(new LaunchProgress(LaunchStep.ValidatingRequirements,
                    $"Missing requirements for {handler.EmulatorName}. Downloading from server..."));

                await DownloadRequirementsAsync(handler, game.Platform, emulatorPath, progress, ct);

                // Re-validate after downloading
                await handler.ValidateRequirementsAsync(emulatorPath, ct);
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

    private async Task<string> DownloadAndInstallEmulatorAsync(
        IEmulatorHandler handler, PlatformType platform,
        IProgress<LaunchProgress> progress, CancellationToken ct)
    {
        var rid = GetCurrentRuntimeId();
        progress.Report(new LaunchProgress(LaunchStep.DownloadingEmulator,
            $"Downloading {handler.EmulatorName} for {rid}..."));

        var installDir = await _settings.GetInstallDirectoryAsync();
        var emuDir = Path.Combine(installDir, "emulators", handler.EmulatorName.ToLower());
        var archivePath = Path.Combine(installDir, "temp", $"{handler.EmulatorName}-{rid}.zip");

        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        Directory.CreateDirectory(emuDir);

        // Download from server
        await using var stream = await _api.GetEmulatorDownloadStreamAsync(platform, rid, ct);
        await using var fileStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);
        fileStream.Close();

        // Install (extract)
        progress.Report(new LaunchProgress(LaunchStep.InstallingEmulator,
            $"Installing {handler.EmulatorName}..."));
        await handler.InstallAsync(archivePath, emuDir, new Progress<double>(), ct);

        // Clean up archive
        try { File.Delete(archivePath); } catch { }

        // Track in local DB
        var emulatorPath = await handler.FindInstalledPathAsync(ct);
        if (emulatorPath is not null)
        {
            using var db = CreateDb();
            db.InstalledEmulators.Add(new InstalledEmulator
            {
                Name = handler.EmulatorName,
                Platform = platform,
                InstallPath = emulatorPath,
            });
            await db.SaveChangesAsync(ct);
        }

        return emulatorPath
            ?? throw new InvalidOperationException($"Failed to locate {handler.EmulatorName} after installation.");
    }

    /// <summary>
    /// Downloads firmware and BIOS files from the server for a given platform,
    /// then installs them into the emulator directory.
    /// </summary>
    private async Task DownloadRequirementsAsync(
        IEmulatorHandler handler, PlatformType platform, string emulatorPath,
        IProgress<LaunchProgress> progress, CancellationToken ct)
    {
        var installDir = await _settings.GetInstallDirectoryAsync();
        var tempDir = Path.Combine(installDir, "temp");
        Directory.CreateDirectory(tempDir);

        // Download and install firmware
        var firmwareList = await _api.GetFirmwareAsync(platform, ct);
        foreach (var fw in firmwareList)
        {
            progress.Report(new LaunchProgress(LaunchStep.ValidatingRequirements,
                $"Downloading firmware v{fw.Version}..."));

            var tempPath = Path.Combine(tempDir, $"firmware_{fw.Id}");
            await using var fwStream = await _api.DownloadFirmwareAsync(fw.Id, ct);
            await using var fwFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
            await fwStream.CopyToAsync(fwFile, ct);
            fwFile.Close();

            await handler.InstallFirmwareAsync(emulatorPath, tempPath, ct);
            try { File.Delete(tempPath); } catch { }
        }

        // Download and install BIOS files
        var biosList = await _api.GetBiosFilesAsync(platform, ct);
        foreach (var bios in biosList)
        {
            progress.Report(new LaunchProgress(LaunchStep.ValidatingRequirements,
                $"Downloading {bios.FileName}..."));

            // Save with the original filename so the handler places it correctly
            var tempPath = Path.Combine(tempDir, bios.FileName);
            await using var biosStream = await _api.DownloadBiosAsync(bios.Id, ct);
            await using var biosFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
            await biosStream.CopyToAsync(biosFile, ct);
            biosFile.Close();

            // Use handler-specific install if available, otherwise copy to emulator dir
            if (handler is Grimoire.Emulators.Handlers.MelonDSHandler melonds)
                await melonds.InstallBiosAsync(emulatorPath, tempPath, ct);
            else if (handler is Grimoire.Emulators.Handlers.RyubingHandler ryubing)
                await ryubing.InstallKeysAsync(emulatorPath, tempPath, ct);
            else
            {
                // Generic: copy to emulator directory
                var emuDir = Path.GetDirectoryName(emulatorPath)!;
                File.Copy(tempPath, Path.Combine(emuDir, bios.FileName), overwrite: true);
            }

            try { File.Delete(tempPath); } catch { }
        }
    }

    private static string GetCurrentRuntimeId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx-arm64";
        return "unknown";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
