namespace Grimoire.Shared.Models;

public record UpdateInfo(
    int Id,
    int GameId,
    string Title,
    string Version,
    long FileSize
);
