using Grimoire.Shared.Enums;
using Grimoire.Shared.Models;

namespace Grimoire.Shared.DTOs;

public record GameDetailDto(
    int Id,
    string Title,
    PlatformType Platform,
    string? Description,
    string? CoverImageUrl,
    long FileSize,
    string? FileHash,
    IReadOnlyList<DlcInfo> Dlcs,
    IReadOnlyList<UpdateInfo> Updates
);
