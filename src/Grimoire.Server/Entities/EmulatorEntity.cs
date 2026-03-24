using Grimoire.Shared.Enums;

namespace Grimoire.Server.Entities;

public class EmulatorEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public PlatformType Platform { get; set; }
    public required string Version { get; set; }
    public string? DownloadUrl { get; set; }
    public required string ExecutableName { get; set; }
}
