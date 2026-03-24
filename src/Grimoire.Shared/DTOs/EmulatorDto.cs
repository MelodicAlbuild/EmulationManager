using Grimoire.Shared.Enums;

namespace Grimoire.Shared.DTOs;

public record EmulatorDto(
    int Id,
    string Name,
    PlatformType Platform,
    string Version,
    string ExecutableName
);
