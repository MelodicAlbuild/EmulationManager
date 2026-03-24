using Grimoire.Shared.Enums;

namespace Grimoire.Shared.Models;

public record BiosInfo(
    int Id,
    PlatformType Platform,
    string FileName,
    string? Description
);
