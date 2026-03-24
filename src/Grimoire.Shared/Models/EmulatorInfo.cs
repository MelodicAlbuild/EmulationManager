using Grimoire.Shared.Enums;

namespace Grimoire.Shared.Models;

public record EmulatorInfo(
    int Id,
    string Name,
    PlatformType Platform,
    string Version,
    string? DownloadUrl,
    string ExecutableName
);
