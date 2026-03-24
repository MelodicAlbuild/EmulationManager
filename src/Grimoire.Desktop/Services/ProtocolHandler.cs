using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Grimoire.Desktop.Services;

public interface IProtocolHandler
{
    int? ParseLaunchGameId(string uri);
    void RegisterProtocol(string executablePath);
    bool IsProtocolRegistered();
}

public class ProtocolHandler : IProtocolHandler
{
    private const string Protocol = "grimoire";

    public int? ParseLaunchGameId(string uri)
    {
        // Format: grimoire://launch/{gameId}
        if (!uri.StartsWith($"{Protocol}://", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = uri[$"{Protocol}://".Length..].Trim('/');
        var parts = path.Split('/');

        if (parts.Length >= 2 && parts[0].Equals("launch", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(parts[1], out var gameId))
                return gameId;
        }

        return null;
    }

    public void RegisterProtocol(string executablePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            RegisterWindows(executablePath);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            RegisterLinux(executablePath);
        // macOS requires plist modifications, typically handled by the installer
    }

    public bool IsProtocolRegistered()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return IsRegisteredWindows();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return IsRegisteredLinux();
        return false;
    }

    private static void RegisterWindows(string executablePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Protocol}");
        key.SetValue("", $"URL:{Protocol} Protocol");
        key.SetValue("URL Protocol", "");

        using var iconKey = key.CreateSubKey("DefaultIcon");
        iconKey.SetValue("", $"\"{executablePath}\",1");

        using var commandKey = key.CreateSubKey(@"shell\open\command");
        commandKey.SetValue("", $"\"{executablePath}\" \"%1\"");
    }

    private static bool IsRegisteredWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{Protocol}");
        return key is not null;
    }

    private static void RegisterLinux(string executablePath)
    {
        var desktopEntry = $"""
            [Desktop Entry]
            Name=Grimoire
            Exec={executablePath} %u
            Type=Application
            MimeType=x-scheme-handler/{Protocol};
            NoDisplay=true
            """;

        var appsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "applications");
        Directory.CreateDirectory(appsDir);

        var desktopFile = Path.Combine(appsDir, $"{Protocol}.desktop");
        File.WriteAllText(desktopFile, desktopEntry);

        // Register with xdg-mime
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "xdg-mime",
                Arguments = $"default {Protocol}.desktop x-scheme-handler/{Protocol}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(psi)?.WaitForExit(5000);
        }
        catch
        {
            // xdg-mime may not be available
        }
    }

    private static bool IsRegisteredLinux()
    {
        var desktopFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "applications", $"{Protocol}.desktop");
        return File.Exists(desktopFile);
    }
}
