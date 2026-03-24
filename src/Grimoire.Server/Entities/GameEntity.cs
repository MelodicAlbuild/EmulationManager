using Grimoire.Shared.Enums;

namespace Grimoire.Server.Entities;

public class GameEntity
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public PlatformType Platform { get; set; }
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }
    public required string FilePath { get; set; }
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<DlcEntity> Dlcs { get; set; } = [];
    public List<UpdateEntity> Updates { get; set; } = [];
}
