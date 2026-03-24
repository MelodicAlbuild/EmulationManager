namespace Grimoire.Server.Entities;

public class UpdateEntity
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public required string Title { get; set; }
    public required string Version { get; set; }
    public required string FilePath { get; set; }
    public long FileSize { get; set; }

    public GameEntity Game { get; set; } = null!;
}
