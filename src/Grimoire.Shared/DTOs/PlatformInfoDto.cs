using Grimoire.Shared.Enums;

namespace Grimoire.Shared.DTOs;

public record PlatformInfoDto(
    PlatformType Type,
    string DisplayName,
    int GameCount
);
