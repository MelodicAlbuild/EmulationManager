using Grimoire.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grimoire.Desktop.Data;

public class LocalDbContext : DbContext
{
    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options) { }

    public DbSet<InstalledEmulator> InstalledEmulators => Set<InstalledEmulator>();
    public DbSet<DownloadedGame> DownloadedGames => Set<DownloadedGame>();
    public DbSet<AppSetting> Settings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstalledEmulator>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Platform).HasConversion<string>();
        });

        modelBuilder.Entity<DownloadedGame>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Platform).HasConversion<string>();
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.HasKey(x => x.Key);
        });
    }
}

public class InstalledEmulator
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public PlatformType Platform { get; set; }
    public required string InstallPath { get; set; }
    public string? Version { get; set; }
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
}

public class DownloadedGame
{
    public int Id { get; set; }
    public int ServerGameId { get; set; }
    public required string Title { get; set; }
    public PlatformType Platform { get; set; }
    public required string LocalPath { get; set; }
    public long FileSize { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
}

public class AppSetting
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}
