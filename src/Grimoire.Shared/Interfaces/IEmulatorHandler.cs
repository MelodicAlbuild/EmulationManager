using System.Diagnostics;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Models;

namespace Grimoire.Shared.Interfaces;

/// <summary>
/// Defines the contract for an emulator handler plugin.
/// Each supported emulator implements this interface to provide
/// platform-specific installation, launch, and management behavior.
/// </summary>
public interface IEmulatorHandler
{
    /// <summary>Display name of the emulator (e.g., "Ryubing", "melonDS").</summary>
    string EmulatorName { get; }

    /// <summary>The platform this handler supports.</summary>
    PlatformType Platform { get; }

    /// <summary>File extensions this emulator can open (e.g., [".nsp", ".xci"]).</summary>
    string[] SupportedFileExtensions { get; }

    // -- Discovery --

    /// <summary>Finds the installed emulator's executable path, or null if not found.</summary>
    Task<string?> FindInstalledPathAsync(CancellationToken ct = default);

    /// <summary>Checks whether the emulator is installed on this system.</summary>
    Task<bool> IsInstalledAsync(CancellationToken ct = default);

    /// <summary>Gets the installed version string, or null if not installed.</summary>
    Task<string?> GetInstalledVersionAsync(CancellationToken ct = default);

    // -- Installation --

    /// <summary>Gets the download URL for the latest emulator release for the current OS.</summary>
    Task<string> GetDownloadUrlAsync(CancellationToken ct = default);

    /// <summary>Installs the emulator from a downloaded archive.</summary>
    Task InstallAsync(string downloadedArchivePath, string installDirectory,
        IProgress<double> progress, CancellationToken ct = default);

    // -- Launch --

    /// <summary>Builds the ProcessStartInfo to launch a game with this emulator.</summary>
    ProcessStartInfo BuildLaunchArgs(string emulatorPath, string romPath, LaunchOptions options);

    // -- Save Management --

    /// <summary>Gets the save directory path for a specific game.</summary>
    string GetSaveDirectory(string emulatorBasePath, string gameId);

    // -- Platform-Specific Content --

    /// <summary>Installs DLC content for a game. No-op for platforms without DLC support.</summary>
    Task InstallDlcAsync(string emulatorPath, string dlcFilePath, CancellationToken ct = default);

    /// <summary>Installs a game update. No-op for platforms without update support.</summary>
    Task InstallUpdateAsync(string emulatorPath, string updateFilePath, CancellationToken ct = default);

    /// <summary>Installs firmware files. No-op for platforms without firmware requirements.</summary>
    Task InstallFirmwareAsync(string emulatorPath, string firmwarePath, CancellationToken ct = default);

    /// <summary>Validates that all requirements (BIOS, firmware, keys) are met.</summary>
    Task ValidateRequirementsAsync(string emulatorPath, CancellationToken ct = default);
}
