using Grimoire.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlatformsController : ControllerBase
{
    private readonly IGameService _gameService;

    public PlatformsController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlatforms()
    {
        var platforms = await _gameService.GetPlatformStatsAsync();
        return Ok(platforms);
    }
}
