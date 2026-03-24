using System.Diagnostics;
using System.Runtime.InteropServices;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;
using Grimoire.Shared.Models;

namespace Grimoire.Emulators.Handlers;

public class RyubingHandler : IEmulatorHandler
{
    public string EmulatorName => "Ryubing";
    public PlatformType Platform => PlatformType.NintendoSwitch;
    public string[] SupportedFileExtensions => [".nsp", ".xci", ".nca"];

    public Task<string?> FindInstalledPathAsync(CancellationToken ct = default)
    {
        // Search common installation paths per OS
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
        // TODO: Parse version from emulator executable or config
        return Task.FromResult<string?>(null);
    }

    public Task<string> GetDownloadUrlAsync(CancellationToken ct = default)
    {
        // TODO: Query Ryubing GitHub Releases API for latest release matching current OS
        // https://api.github.com/repos/Ryubing/Ryujinx/releases/latest
        throw new NotImplementedException("Ryubing download URL resolution not yet implemented");
    }

    public Task InstallAsync(string downloadedArchivePath, string installDirectory,
        IProgress<double> progress, CancellationToken ct = default)
    {
        // TODO: Extract ZIP/tar.gz to installDirectory, report progress
        throw new NotImplementedException("Ryubing installation not yet implemented");
    }

    public ProcessStartInfo BuildLaunchArgs(string emulatorPath, string romPath, LaunchOptions options)
    {
        var args = $"\"{romPath}\"";
        if (options.Fullscreen)
            args += " --fullscreen";
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
        // Ryubing stores saves in: <portable_dir>/bis/user/save/<titleId>
        return Path.Combine(Path.GetDirectoryName(emulatorBasePath)!, "bis", "user", "save", gameId);
    }

    public Task InstallDlcAsync(string emulatorPath, string dlcFilePath, CancellationToken ct = default)
    {
        // TODO: Copy DLC NSP to Ryubing's DLC directory, update metadata
        throw new NotImplementedException("Ryubing DLC installation not yet implemented");
    }

    public Task InstallUpdateAsync(string emulatorPath, string updateFilePath, CancellationToken ct = default)
    {
        // TODO: Install game update via Ryubing's update mechanism
        throw new NotImplementedException("Ryubing update installation not yet implemented");
    }

    public Task InstallFirmwareAsync(string emulatorPath, string firmwarePath, CancellationToken ct = default)
    {
        // TODO: Extract firmware to Ryubing's firmware directory
        throw new NotImplementedException("Ryubing firmware installation not yet implemented");
    }

    public Task ValidateRequirementsAsync(string emulatorPath, CancellationToken ct = default)
    {
        // TODO: Check for prod.keys in Ryubing's system directory
        var baseDir = Path.GetDirectoryName(emulatorPath)!;
        var keysPath = Path.Combine(baseDir, "system", "prod.keys");
        if (!File.Exists(keysPath))
        {
            throw new FileNotFoundException(
                "prod.keys not found. Switch games require production keys to run.", keysPath);
        }
        return Task.CompletedTask;
    }

    private static string[] GetSearchPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return [
                Path.Combine(localAppData, "Ryujinx", "Ryujinx.exe"),
                Path.Combine(localAppData, "Ryubing", "Ryujinx.exe"),
            ];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return [
                Path.Combine(home, ".config", "Ryujinx", "Ryujinx"),
                "/usr/bin/Ryujinx",
            ];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return [
                "/Applications/Ryujinx.app/Contents/MacOS/Ryujinx",
            ];
        }
        return [];
    }
}
