using System.Diagnostics;
using System.Runtime.InteropServices;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;
using Grimoire.Shared.Models;

namespace Grimoire.Emulators.Handlers;

public class MGBAHandler : IEmulatorHandler
{
    public string EmulatorName => "mGBA";
    public PlatformType Platform => PlatformType.GameBoy;
    public string[] SupportedFileExtensions => [".gb", ".gbc", ".gba"];

    public Task<string?> FindInstalledPathAsync(CancellationToken ct = default)
    {
        // Check Grimoire-managed install directory first
        var grimoirePath = GrimoirePaths.FindEmulatorExecutable("mGBA", "mGBA.exe");
        if (grimoirePath is not null)
            return Task.FromResult<string?>(grimoirePath);

        // Fall back to common system installation paths
        var candidates = GetSearchPaths();
        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return Task.FromResult<string?>(path);
        }
        return Task.FromResult<string?>(null);
    }

    public async Task<bool> IsInstalledAsync(CancellationToken ct = default)
    {
        return await FindInstalledPathAsync(ct) is not null;
    }

    public Task<string?> GetInstalledVersionAsync(CancellationToken ct = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string> GetDownloadUrlAsync(CancellationToken ct = default)
    {
        // Served from Grimoire server, not GitHub
        throw new NotSupportedException("Emulator binaries are served from the Grimoire server.");
    }

    public Task InstallAsync(string downloadedArchivePath, string installDirectory,
        IProgress<double> progress, CancellationToken ct = default)
    {
        return ArchiveInstaller.ExtractZipAsync(downloadedArchivePath, installDirectory, progress, ct);
    }

    public ProcessStartInfo BuildLaunchArgs(string emulatorPath, string romPath, LaunchOptions options)
    {
        var args = $"\"{romPath}\"";
        if (options.Fullscreen)
            args += " -f";
        if (!string.IsNullOrWhiteSpace(options.CustomArgs))
            args += $" {options.CustomArgs}";

        return new ProcessStartInfo
        {
            FileName = emulatorPath,
            Arguments = args,
            UseShellExecute = false
        };
    }

    public string GetSaveDirectory(string emulatorBasePath, string gameId)
    {
        // mGBA stores saves next to the ROM by default, but we organize them
        return Path.Combine(Path.GetDirectoryName(emulatorBasePath)!, "saves", gameId);
    }

    // Game Boy has no DLC/update/firmware management
    public Task InstallDlcAsync(string emulatorPath, string dlcFilePath, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task InstallUpdateAsync(string emulatorPath, string updateFilePath, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task InstallFirmwareAsync(string emulatorPath, string firmwarePath, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ValidateRequirementsAsync(string emulatorPath, CancellationToken ct = default)
    {
        // mGBA has optional BIOS (gba_bios.bin) but runs without it
        return Task.CompletedTask;
    }

    private static string[] GetSearchPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return [
                Path.Combine(programFiles, "mGBA", "mGBA.exe"),
                Path.Combine(localAppData, "mGBA", "mGBA.exe"),
            ];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ["/usr/bin/mgba-qt", "/usr/local/bin/mgba-qt", "/usr/bin/mgba"];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ["/Applications/mGBA.app/Contents/MacOS/mGBA"];
        }
        return [];
    }
}
