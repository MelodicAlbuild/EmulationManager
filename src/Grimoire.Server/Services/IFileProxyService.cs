using Grimoire.Shared.Enums;

namespace Grimoire.Server.Services;

public record FileDownloadInfo(
    string PhysicalPath,
    string FileName,
    long FileSize,
    string ContentType
);

public interface IFileProxyService
{
    Task<FileDownloadInfo?> ResolveFileAsync(DownloadableType type, int id);
}
