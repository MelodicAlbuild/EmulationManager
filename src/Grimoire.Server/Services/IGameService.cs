using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;

namespace Grimoire.Server.Services;

public interface IGameService
{
    Task<IReadOnlyList<GameListDto>> GetGamesAsync(PlatformType? platform = null, string? search = null);
    Task<GameDetailDto?> GetGameDetailAsync(int gameId);
    Task<IReadOnlyList<PlatformInfoDto>> GetPlatformStatsAsync();
    Task<IReadOnlyList<EmulatorDto>> GetEmulatorsAsync();
    Task<EmulatorDto?> GetEmulatorByPlatformAsync(PlatformType platform);
    Task<int> GetGameCountAsync();
}
