using System.Diagnostics;
using System.Runtime.InteropServices;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;
using Grimoire.Shared.Models;

namespace Grimoire.Emulators.Handlers;

public class CitraHandler : IEmulatorHandler
{
    public string EmulatorName => "Citra";
    public PlatformType Platform => PlatformType.Nintendo3DS;
    public string[] SupportedFileExtensions => [".3ds", ".cci", ".cxi", ".cia"];

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
        // TODO: Query PabloMK7/citra GitHub Releases API
        // https://api.github.com/repos/PabloMK7/citra/releases/latest
        throw new NotImplementedException("Citra download URL resolution not yet implemented");
    }

    public Task InstallAsync(string downloadedArchivePath, string installDirectory,
        IProgress<double> progress, CancellationToken ct = default)
    {
        throw new NotImplementedException("Citra installation not yet implemented");
    }

    public ProcessStartInfo BuildLaunchArgs(string emulatorPath, string romPath, LaunchOptions options)
    {
        var args = $"-g \"{romPath}\"";
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
        // Citra stores saves in its sdmc directory under the title structure
        var baseDir = Path.GetDirectoryName(emulatorBasePath)!;
        return Path.Combine(baseDir, "sdmc", "Nintendo 3DS", "00000000000000000000000000000000",
            "00000000000000000000000000000000", "title", gameId);
    }

    public Task InstallDlcAsync(string emulatorPath, string dlcFilePath, CancellationToken ct = default)
    {
        // TODO: Place CIA DLC files in Citra's install directory
        throw new NotImplementedException("Citra DLC installation not yet implemented");
    }

    public Task InstallUpdateAsync(string emulatorPath, string updateFilePath, CancellationToken ct = default)
    {
        // TODO: Place update CIA in Citra's install directory
        throw new NotImplementedException("Citra update installation not yet implemented");
    }

    public Task InstallFirmwareAsync(string emulatorPath, string firmwarePath, CancellationToken ct = default)
    {
        // Citra doesn't require separate firmware
        return Task.CompletedTask;
    }

    public Task ValidateRequirementsAsync(string emulatorPath, CancellationToken ct = default)
    {
        // Citra has no mandatory external BIOS/firmware requirements
        return Task.CompletedTask;
    }

    private static string[] GetSearchPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return [
                Path.Combine(localAppData, "citra-emu", "citra-qt.exe"),
                Path.Combine(localAppData, "Citra", "citra-qt.exe"),
            ];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ["/usr/bin/citra-qt", "/usr/local/bin/citra-qt"];
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ["/Applications/Citra.app/Contents/MacOS/citra-qt"];
        }
        return [];
    }
}
