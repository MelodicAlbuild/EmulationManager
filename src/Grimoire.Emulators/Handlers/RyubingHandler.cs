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
        return ArchiveInstaller.ExtractZipAsync(downloadedArchivePath, installDirectory, progress, ct);
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
        // Place DLC NSP alongside emulator for Ryubing to detect
        var baseDir = Path.GetDirectoryName(emulatorPath)!;
        var dlcDir = Path.Combine(baseDir, "sdcard", "dlc");
        Directory.CreateDirectory(dlcDir);
        File.Copy(dlcFilePath, Path.Combine(dlcDir, Path.GetFileName(dlcFilePath)), overwrite: true);
        return Task.CompletedTask;
    }

    public Task InstallUpdateAsync(string emulatorPath, string updateFilePath, CancellationToken ct = default)
    {
        var baseDir = Path.GetDirectoryName(emulatorPath)!;
        var updateDir = Path.Combine(baseDir, "sdcard", "updates");
        Directory.CreateDirectory(updateDir);
        File.Copy(updateFilePath, Path.Combine(updateDir, Path.GetFileName(updateFilePath)), overwrite: true);
        return Task.CompletedTask;
    }

    public Task InstallFirmwareAsync(string emulatorPath, string firmwarePath, CancellationToken ct = default)
    {
        var baseDir = Path.GetDirectoryName(emulatorPath)!;
        var fwDir = Path.Combine(baseDir, "bis", "system", "Contents", "registered");
        Directory.CreateDirectory(fwDir);
        // Firmware comes as a zip � extract it
        if (firmwarePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            System.IO.Compression.ZipFile.ExtractToDirectory(firmwarePath, fwDir, overwriteFiles: true);
        else
            File.Copy(firmwarePath, Path.Combine(fwDir, Path.GetFileName(firmwarePath)), overwrite: true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Installs a keys file (e.g., prod.keys) into the emulator's system directory.
    /// This is called by LaunchService when validation detects missing keys.
    /// </summary>
    public Task InstallKeysAsync(string emulatorPath, string keysFilePath, CancellationToken ct = default)
    {
        var baseDir = Path.GetDirectoryName(emulatorPath)!;
        var systemDir = Path.Combine(baseDir, "system");
        Directory.CreateDirectory(systemDir);
        File.Copy(keysFilePath, Path.Combine(systemDir, Path.GetFileName(keysFilePath)), overwrite: true);
        return Task.CompletedTask;
    }

    public Task ValidateRequirementsAsync(string emulatorPath, CancellationToken ct = default)
    {
        var baseDir = Path.GetDirectoryName(emulatorPath)!;
        var systemDir = Path.Combine(baseDir, "system");

        var missing = new List<string>();
        if (!File.Exists(Path.Combine(systemDir, "prod.keys")))
            missing.Add("prod.keys");
        if (!File.Exists(Path.Combine(systemDir, "title.keys")))
            missing.Add("title.keys");

        if (missing.Count > 0)
        {
            throw new FileNotFoundException(
                $"Missing key files: {string.Join(", ", missing)}. Switch games require prod.keys and title.keys to run.");
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
