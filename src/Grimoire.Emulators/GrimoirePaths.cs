using System.Runtime.InteropServices;

namespace Grimoire.Emulators;

/// <summary>
/// Resolves the Grimoire install directory and searches for emulator executables
/// within the Grimoire-managed emulator directories.
/// </summary>
public static class GrimoirePaths
{
    /// <summary>
    /// Returns the base Grimoire install directory (%LocalAppData%/Grimoire on Windows,
    /// ~/.local/share/Grimoire on Linux, ~/Library/Application Support/Grimoire on macOS).
    /// </summary>
    public static string GetBaseDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Grimoire");
    }

    /// <summary>
    /// Searches the Grimoire emulators directory for an executable by name.
    /// Looks in: {base}/emulators/{emulatorName}/ recursively.
    /// </summary>
    public static string? FindEmulatorExecutable(string emulatorName, string executableName)
    {
        var emuDir = Path.Combine(GetBaseDirectory(), "emulators", emulatorName.ToLower());
        if (!Directory.Exists(emuDir))
            return null;

        // Search recursively for the executable
        try
        {
            var files = Directory.GetFiles(emuDir, executableName, SearchOption.AllDirectories);
            if (files.Length > 0)
                return files[0];

            // On Linux, executable might not have an extension
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(executableName);
                files = Directory.GetFiles(emuDir, nameWithoutExt, SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
            }
        }
        catch (DirectoryNotFoundException) { }

        return null;
    }
}
