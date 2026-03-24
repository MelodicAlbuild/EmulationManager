using Grimoire.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    private readonly IServiceProvider _services;

    public const string ServerUrlKey = "ServerUrl";
    public const string InstallDirectoryKey = "InstallDirectory";

    public SettingsService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<string?> GetAsync(string key)
    {
        using var db = _services.GetRequiredService<LocalDbContext>();
        var setting = await db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string value)
    {
        using var db = _services.GetRequiredService<LocalDbContext>();
        var setting = await db.Settings.FindAsync(key);
        if (setting is not null)
        {
            setting.Value = value;
        }
        else
        {
            db.Settings.Add(new AppSetting { Key = key, Value = value });
        }
        await db.SaveChangesAsync();
    }

    public async Task<string> GetServerUrlAsync()
    {
        return await GetAsync(ServerUrlKey) ?? "https://emu.melodicalbuild.com";
    }

    public async Task SetServerUrlAsync(string url)
    {
        await SetAsync(ServerUrlKey, url.TrimEnd('/'));
    }

    public async Task<string> GetInstallDirectoryAsync()
    {
        var dir = await GetAsync(InstallDirectoryKey);
        if (dir is not null) return dir;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Grimoire");
    }

    public async Task<bool> IsConfiguredAsync()
    {
        return await GetAsync(ServerUrlKey) is not null;
    }
}
