using EmulationManager.Emulators.Handlers;
using EmulationManager.Shared.Enums;
using EmulationManager.Shared.Interfaces;
using EmulationManager.Shared.Models;

namespace EmulationManager.Emulators.Tests;

public class HandlerRegistryTests
{
    private static EmulatorHandlerRegistry CreateRegistry()
    {
        IEmulatorHandler[] handlers = [new RyubingHandler(), new MelonDSHandler(), new CitraHandler()];
        return new EmulatorHandlerRegistry(handlers);
    }

    [Fact]
    public void Registry_SupportedPlatforms_ContainsAllThree()
    {
        var registry = CreateRegistry();
        Assert.Contains(PlatformType.NintendoSwitch, registry.SupportedPlatforms);
        Assert.Contains(PlatformType.NintendoDS, registry.SupportedPlatforms);
        Assert.Contains(PlatformType.Nintendo3DS, registry.SupportedPlatforms);
    }

    [Fact]
    public void Registry_GetHandler_ReturnsCorrectHandler()
    {
        var registry = CreateRegistry();
        var handler = registry.GetHandler(PlatformType.NintendoSwitch);
        Assert.Equal("Ryubing", handler.EmulatorName);
    }

    [Fact]
    public void Registry_TryGetHandler_ReturnsTrueForKnown()
    {
        var registry = CreateRegistry();
        Assert.True(registry.TryGetHandler(PlatformType.NintendoDS, out var handler));
        Assert.NotNull(handler);
        Assert.Equal("melonDS", handler!.EmulatorName);
    }

    [Fact]
    public void Registry_AllHandlers_ReturnsThree()
    {
        var registry = CreateRegistry();
        Assert.Equal(3, registry.AllHandlers.Count);
    }
}

public class RyubingHandlerTests
{
    private readonly RyubingHandler _handler = new();

    [Fact]
    public void EmulatorName_IsRyubing()
    {
        Assert.Equal("Ryubing", _handler.EmulatorName);
    }

    [Fact]
    public void Platform_IsNintendoSwitch()
    {
        Assert.Equal(PlatformType.NintendoSwitch, _handler.Platform);
    }

    [Fact]
    public void SupportedExtensions_IncludesNspAndXci()
    {
        Assert.Contains(".nsp", _handler.SupportedFileExtensions);
        Assert.Contains(".xci", _handler.SupportedFileExtensions);
    }

    [Fact]
    public void BuildLaunchArgs_IncludesFullscreen()
    {
        var psi = _handler.BuildLaunchArgs("/path/to/ryujinx", "/games/zelda.nsp", new LaunchOptions());
        Assert.Contains("--fullscreen", psi.Arguments);
        Assert.Contains("zelda.nsp", psi.Arguments);
    }

    [Fact]
    public void BuildLaunchArgs_NoFullscreen_WhenDisabled()
    {
        var psi = _handler.BuildLaunchArgs("/path/to/ryujinx", "/games/zelda.nsp", new LaunchOptions(Fullscreen: false));
        Assert.DoesNotContain("--fullscreen", psi.Arguments);
    }

    [Fact]
    public void BuildLaunchArgs_IncludesCustomArgs()
    {
        var psi = _handler.BuildLaunchArgs("/path/to/ryujinx", "/games/zelda.nsp",
            new LaunchOptions(CustomArgs: "--verbose"));
        Assert.Contains("--verbose", psi.Arguments);
    }

    [Fact]
    public void GetSaveDirectory_ReturnsExpectedPath()
    {
        var saveDir = _handler.GetSaveDirectory("/apps/ryujinx/Ryujinx.exe", "01007EF00011E000");
        Assert.Contains("bis", saveDir);
        Assert.Contains("save", saveDir);
        Assert.Contains("01007EF00011E000", saveDir);
    }
}

public class MelonDSHandlerTests
{
    private readonly MelonDSHandler _handler = new();

    [Fact]
    public void EmulatorName_IsMelonDS()
    {
        Assert.Equal("melonDS", _handler.EmulatorName);
    }

    [Fact]
    public void Platform_IsNintendoDS()
    {
        Assert.Equal(PlatformType.NintendoDS, _handler.Platform);
    }

    [Fact]
    public void SupportedExtensions_IncludesNds()
    {
        Assert.Contains(".nds", _handler.SupportedFileExtensions);
    }

    [Fact]
    public void BuildLaunchArgs_UsesMinusFForFullscreen()
    {
        var psi = _handler.BuildLaunchArgs("/path/to/melonDS", "/games/pokemon.nds", new LaunchOptions());
        Assert.Contains("-f", psi.Arguments);
        Assert.Contains("pokemon.nds", psi.Arguments);
    }

    [Fact]
    public void GetSaveDirectory_ReturnsExpectedPath()
    {
        var saveDir = _handler.GetSaveDirectory("/apps/melonDS/melonDS.exe", "pokemon_black");
        Assert.Contains("saves", saveDir);
        Assert.Contains("pokemon_black", saveDir);
    }

    [Fact]
    public async Task InstallDlc_IsNoOp()
    {
        // DS doesn't have DLC, should complete without error
        await _handler.InstallDlcAsync("/path", "/dlc", CancellationToken.None);
    }

    [Fact]
    public async Task InstallUpdate_IsNoOp()
    {
        await _handler.InstallUpdateAsync("/path", "/update", CancellationToken.None);
    }
}

public class CitraHandlerTests
{
    private readonly CitraHandler _handler = new();

    [Fact]
    public void EmulatorName_IsCitra()
    {
        Assert.Equal("Citra", _handler.EmulatorName);
    }

    [Fact]
    public void Platform_IsNintendo3DS()
    {
        Assert.Equal(PlatformType.Nintendo3DS, _handler.Platform);
    }

    [Fact]
    public void SupportedExtensions_Includes3dsAndCia()
    {
        Assert.Contains(".3ds", _handler.SupportedFileExtensions);
        Assert.Contains(".cia", _handler.SupportedFileExtensions);
    }

    [Fact]
    public void BuildLaunchArgs_UsesGFlag()
    {
        var psi = _handler.BuildLaunchArgs("/path/to/citra-qt", "/games/pokemon.3ds", new LaunchOptions());
        Assert.Contains("-g", psi.Arguments);
        Assert.Contains("-f", psi.Arguments);
        Assert.Contains("pokemon.3ds", psi.Arguments);
    }

    [Fact]
    public void GetSaveDirectory_ContainsSdmcPath()
    {
        var saveDir = _handler.GetSaveDirectory("/apps/citra/citra-qt.exe", "00040000001B5000");
        Assert.Contains("sdmc", saveDir);
        Assert.Contains("00040000001B5000", saveDir);
    }

    [Fact]
    public async Task ValidateRequirements_DoesNotThrow()
    {
        // Citra has no mandatory BIOS requirements
        await _handler.ValidateRequirementsAsync("/nonexistent/path");
    }

    [Fact]
    public async Task InstallFirmware_IsNoOp()
    {
        await _handler.InstallFirmwareAsync("/path", "/fw", CancellationToken.None);
    }
}
