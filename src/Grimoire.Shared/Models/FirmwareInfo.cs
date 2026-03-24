using Grimoire.Shared.Enums;

namespace Grimoire.Shared.Models;

public record FirmwareInfo(
    int Id,
    PlatformType Platform,
    string Version,
    string? DownloadUrl
);
