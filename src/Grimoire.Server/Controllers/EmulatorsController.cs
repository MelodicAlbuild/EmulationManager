using Grimoire.Server.Services;
using Grimoire.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmulatorsController : ControllerBase
{
    private readonly IGameService _gameService;

    public EmulatorsController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmulators()
    {
        var emulators = await _gameService.GetEmulatorsAsync();
        return Ok(emulators);
    }

    [HttpGet("{platform}")]
    public async Task<IActionResult> GetEmulator(PlatformType platform)
    {
        var emulator = await _gameService.GetEmulatorByPlatformAsync(platform);
        if (emulator is null)
            return NotFound();
        return Ok(emulator);
    }
}
