using Grimoire.Shared.Enums;

namespace Grimoire.Shared.DTOs;

public record DownloadProgressDto(
    string DownloadId,
    long BytesDownloaded,
    long TotalBytes,
    DownloadStatus Status
)
{
    public double ProgressPercent => TotalBytes > 0
        ? (double)BytesDownloaded / TotalBytes * 100.0
        : 0.0;
}
