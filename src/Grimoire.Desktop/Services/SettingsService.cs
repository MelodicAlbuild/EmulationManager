using Grimoire.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace Grimoire.Desktop.Services;

public interface ISettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<string> GetServerUrlAsync();
    Task SetServerUrlAsync(string url);
    Task<string> GetInstallDirectoryAsync();
    Task<bool> IsConfiguredAsync();
}

public class SettingsService : ISettingsService
{
    private readonly LocalDbContext _db;

    public const string ServerUrlKey = "ServerUrl";
    public const string InstallDirectoryKey = "InstallDirectory";

    public SettingsService(LocalDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _db.Settings.FindAsync(key);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _db.Settings.FindAsync(key);
        if (setting is not null)
        {
            setting.Value = value;
        }
        else
        {
            _db.Settings.Add(new AppSetting { Key = key, Value = value });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<string> GetServerUrlAsync()
    {
        return await GetAsync(ServerUrlKey) ?? "http://localhost:5038";
    }

    public async Task SetServerUrlAsync(string url)
    {
        await SetAsync(ServerUrlKey, url.TrimEnd('/'));
    }

    public async Task<string> GetInstallDirectoryAsync()
    {
        var dir = await GetAsync(InstallDirectoryKey);
        if (dir is not null) return dir;

        // Default install directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Grimoire");
    }

    public async Task<bool> IsConfiguredAsync()
    {
        return await GetAsync(ServerUrlKey) is not null;
    }
}
