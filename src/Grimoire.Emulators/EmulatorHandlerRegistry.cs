using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;

namespace Grimoire.Emulators;

/// <summary>
/// Registry that maps PlatformType to the correct IEmulatorHandler implementation.
/// Populated via DI by collecting all registered IEmulatorHandler services.
/// </summary>
public class EmulatorHandlerRegistry
{
    private readonly Dictionary<PlatformType, IEmulatorHandler> _handlers;

    public EmulatorHandlerRegistry(IEnumerable<IEmulatorHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.Platform);
    }

    public IEmulatorHandler GetHandler(PlatformType platform)
    {
        if (!_handlers.TryGetValue(platform, out var handler))
            throw new NotSupportedException($"No emulator handler registered for platform: {platform}");
        return handler;
    }

    public bool TryGetHandler(PlatformType platform, out IEmulatorHandler? handler)
    {
        return _handlers.TryGetValue(platform, out handler);
    }

    public IReadOnlyCollection<PlatformType> SupportedPlatforms => _handlers.Keys.ToList();

    public IReadOnlyCollection<IEmulatorHandler> AllHandlers => _handlers.Values.ToList();
}
