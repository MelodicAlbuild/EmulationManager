using Grimoire.Server.Data;
using Grimoire.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Grimoire.Server.Services;

public class FileProxyService : IFileProxyService
{
    private readonly GrimoireDbContext _db;
    private readonly StorageOptions _storage;

    public FileProxyService(GrimoireDbContext db, IOptions<StorageOptions> storage)
    {
        _db = db;
        _storage = storage.Value;
    }

    public async Task<FileDownloadInfo?> ResolveFileAsync(DownloadableType type, int id)
    {
        return type switch
        {
            DownloadableType.Game => await ResolveGameAsync(id),
            DownloadableType.Dlc => await ResolveDlcAsync(id),
            DownloadableType.Update => await ResolveUpdateAsync(id),
            DownloadableType.Firmware => await ResolveFirmwareAsync(id),
            DownloadableType.Bios => await ResolveBiosAsync(id),
            _ => null
        };
    }

    private async Task<FileDownloadInfo?> ResolveGameAsync(int id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game is null) return null;

        var physicalPath = ResolvePath(_storage.GamesBasePath, game.FilePath);
        return new FileDownloadInfo(
            physicalPath,
            Path.GetFileName(game.FilePath),
            game.FileSize,
            GetContentType(game.FilePath)
        );
    }

    private async Task<FileDownloadInfo?> ResolveDlcAsync(int id)
    {
        var dlc = await _db.Dlcs.FindAsync(id);
        if (dlc is null) return null;

        var physicalPath = ResolvePath(_storage.DlcBasePath, dlc.FilePath);
        return new FileDownloadInfo(
            physicalPath,
            Path.GetFileName(dlc.FilePath),
            dlc.FileSize,
            GetContentType(dlc.FilePath)
        );
    }

    private async Task<FileDownloadInfo?> ResolveUpdateAsync(int id)
    {
        var update = await _db.Updates.FindAsync(id);
        if (update is null) return null;

        var physicalPath = ResolvePath(_storage.UpdatesBasePath, update.FilePath);
        return new FileDownloadInfo(
            physicalPath,
            Path.GetFileName(update.FilePath),
            update.FileSize,
            GetContentType(update.FilePath)
        );
    }

    private async Task<FileDownloadInfo?> ResolveFirmwareAsync(int id)
    {
        var firmware = await _db.Firmwares.FindAsync(id);
        if (firmware is null) return null;

        var physicalPath = ResolvePath(_storage.FirmwareBasePath, firmware.FilePath);
        return new FileDownloadInfo(
            physicalPath,
            Path.GetFileName(firmware.FilePath),
            new FileInfo(physicalPath).Length,
            "application/octet-stream"
        );
    }

    private async Task<FileDownloadInfo?> ResolveBiosAsync(int id)
    {
        var bios = await _db.BiosFiles.FindAsync(id);
        if (bios is null) return null;

        var physicalPath = ResolvePath(_storage.BiosBasePath, bios.FilePath);
        return new FileDownloadInfo(
            physicalPath,
            Path.GetFileName(bios.FilePath),
            new FileInfo(physicalPath).Length,
            "application/octet-stream"
        );
    }

    private static string ResolvePath(string basePath, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return relativePath;
        return Path.Combine(basePath, relativePath);
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".nsp" or ".xci" or ".nca" => "application/octet-stream",
            ".nds" or ".dsi" => "application/octet-stream",
            ".3ds" or ".cci" or ".cxi" or ".cia" => "application/octet-stream",
            ".zip" => "application/zip",
            ".7z" => "application/x-7z-compressed",
            _ => "application/octet-stream"
        };
    }
}
