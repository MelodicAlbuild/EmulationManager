namespace Grimoire.Shared.Models;

public record DlcInfo(
    int Id,
    int GameId,
    string Title,
    string? Version,
    long FileSize
);
