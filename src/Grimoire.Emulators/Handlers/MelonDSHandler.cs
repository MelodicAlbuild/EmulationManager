using System.Diagnostics;
using System.Runtime.InteropServices;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;
using Grimoire.Shared.Models;

namespace Grimoire.Emulators.Handlers;

public class MelonDSHandler : IEmulatorHandler
{
    public string EmulatorName => "melonDS";
    public PlatformType Platform => PlatformType.NintendoDS;
    public string[] SupportedFileExtensions => [".nds", ".dsi"];

    public Task<string?> FindInstalledPathAsync(CancellationToken ct = default)
    {
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
        // TODO: Query melonDS GitHub Releases API
        // https://api.github.com/repos/melonDS-emu/melonDS/releases/latest
        throw new NotImplementedException("melonDS download URL resolution not yet implemented");
    }

    public Task InstallAsync(string downloadedArchivePath, string installDirectory,
        IProgress<double> progress, CancellationToken ct = default)
    {
        throw new NotImplementedException("melonDS installation not yet implemented");
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
        return Path.Combine(Path.GetDirectoryName(emulatorBasePath)!, "saves", gameId);
    }

    // DS has no DLC/update/firmware management through the emulator
    public Task InstallDlcAsync(string emulatorPath, string dlcFilePath, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task InstallUpdateAsync(string emulatorPath, string updateFilePath, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task InstallFirmwareAsync(string emulatorPath, string firmwarePath, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ValidateRequirementsAsync(string emulatorPath, CancellationToken ct = default)
    {
        var baseDir = Path.GetDirectoryName(emulatorPath)!;
        var requiredBios = new[] { "bios7.bin", "bios9.bin", "firmware.bin" };
        var missing = requiredBios.Where(f => !File.Exists(Path.Combine(baseDir, f))).ToList();

        if (missing.Count > 0)
        {
            throw new FileNotFoundException(
                $"Required BIOS files missing: {string.Join(", ", missing)}. " +
                "DS emulation requires bios7.bin, bios9.bin, and firmware.bin.");
        }
        return Task.CompletedTask;
    }

    private static string[] GetSearchPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return [
                Path.Combine(programFiles, "melonDS", "melonDS.exe"),
            ];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ["/usr/bin/melonDS", "/usr/local/bin/melonDS"];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ["/Applications/melonDS.app/Contents/MacOS/melonDS"];
        }
        return [];
    }
}
