namespace Grimoire.Shared.Models;

public record LaunchOptions(
    bool Fullscreen = true,
    string? CustomArgs = null
);
