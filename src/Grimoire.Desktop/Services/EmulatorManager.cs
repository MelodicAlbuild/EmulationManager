using Grimoire.Emulators;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;

namespace Grimoire.Desktop.Services;

public interface IEmulatorManager
{
    Task<string?> GetInstalledPathAsync(PlatformType platform);
    Task<bool> IsInstalledAsync(PlatformType platform);
    IEmulatorHandler? GetHandler(PlatformType platform);
    IReadOnlyCollection<PlatformType> SupportedPlatforms { get; }
}

public class EmulatorManager : IEmulatorManager
{
    private readonly EmulatorHandlerRegistry _registry;

    public EmulatorManager(EmulatorHandlerRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyCollection<PlatformType> SupportedPlatforms => _registry.SupportedPlatforms;

    public IEmulatorHandler? GetHandler(PlatformType platform)
    {
        return _registry.TryGetHandler(platform, out var handler) ? handler : null;
    }

    public async Task<string?> GetInstalledPathAsync(PlatformType platform)
    {
        var handler = GetHandler(platform);
        if (handler is null) return null;
        return await handler.FindInstalledPathAsync();
    }

    public async Task<bool> IsInstalledAsync(PlatformType platform)
    {
        var handler = GetHandler(platform);
        if (handler is null) return false;
        return await handler.IsInstalledAsync();
    }
}
