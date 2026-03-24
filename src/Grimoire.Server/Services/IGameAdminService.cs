using Grimoire.Shared.Enums;

namespace Grimoire.Server.Services;

public record GameCreateDto(
    string Title,
    PlatformType Platform,
    string? Description,
    string FilePath,
    long FileSize
);

public record GameUpdateDto(
    string Title,
    PlatformType Platform,
    string? Description,
    string FilePath,
    long FileSize
);

public interface IGameAdminService
{
    Task<int> CreateGameAsync(GameCreateDto dto);
    Task<bool> UpdateGameAsync(int id, GameUpdateDto dto);
    Task<bool> DeleteGameAsync(int id);
}
