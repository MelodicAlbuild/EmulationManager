using Grimoire.Shared.Enums;

namespace Grimoire.Shared.Models;

public record GameInfo(
    int Id,
    string Title,
    PlatformType Platform,
    string? Description,
    string? CoverImageUrl,
    long FileSize,
    string? FileHash
);
