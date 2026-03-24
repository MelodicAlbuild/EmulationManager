using Grimoire.Shared.Enums;

namespace Grimoire.Server.Entities;

public class BiosEntity
{
    public int Id { get; set; }
    public PlatformType Platform { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public string? Description { get; set; }
}
