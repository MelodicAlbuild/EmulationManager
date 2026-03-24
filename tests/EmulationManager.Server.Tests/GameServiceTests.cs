using EmulationManager.Server.Data;
using EmulationManager.Server.Entities;
using EmulationManager.Server.Services;
using EmulationManager.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmulationManager.Server.Tests;

public class GameServiceTests : IDisposable
{
    private readonly EmulationManagerDbContext _db;
    private readonly GameService _service;

    public GameServiceTests()
    {
        var options = new DbContextOptionsBuilder<EmulationManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new EmulationManagerDbContext(options);
        _service = new GameService(_db);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _db.Games.AddRange(
            new GameEntity
            {
                Title = "Zelda BOTW",
                Platform = PlatformType.NintendoSwitch,
                FilePath = "switch/zelda.nsp",
                FileSize = 14_000_000_000,
                Description = "An adventure game",
                Dlcs = [new DlcEntity { Title = "DLC Pack 1", FilePath = "dlc1.nsp", FileSize = 500_000_000 }],
                Updates = [new UpdateEntity { Title = "v1.6.0", Version = "1.6.0", FilePath = "update.nsp", FileSize = 200_000_000 }]
            },
            new GameEntity
            {
                Title = "Mario Odyssey",
                Platform = PlatformType.NintendoSwitch,
                FilePath = "switch/mario.nsp",
                FileSize = 5_700_000_000
            },
            new GameEntity
            {
                Title = "Pokemon Black",
                Platform = PlatformType.NintendoDS,
                FilePath = "ds/pokemon.nds",
                FileSize = 128_000_000
            },
            new GameEntity
            {
                Title = "Fire Emblem Awakening",
                Platform = PlatformType.Nintendo3DS,
                FilePath = "3ds/fire_emblem.3ds",
                FileSize = 1_800_000_000
            }
        );

        _db.Emulators.AddRange(
            new EmulatorEntity { Name = "Ryubing", Platform = PlatformType.NintendoSwitch, Version = "1.0", ExecutableName = "Ryujinx.exe" },
            new EmulatorEntity { Name = "melonDS", Platform = PlatformType.NintendoDS, Version = "1.0", ExecutableName = "melonDS.exe" },
            new EmulatorEntity { Name = "Citra", Platform = PlatformType.Nintendo3DS, Version = "1.0", ExecutableName = "citra-qt.exe" }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsAllGames()
    {
        var games = await _service.GetGamesAsync();
        Assert.Equal(4, games.Count);
    }

    [Fact]
    public async Task GetGamesAsync_FilterByPlatform_ReturnsOnlyMatching()
    {
        var games = await _service.GetGamesAsync(platform: PlatformType.NintendoSwitch);
        Assert.Equal(2, games.Count);
        Assert.All(games, g => Assert.Equal(PlatformType.NintendoSwitch, g.Platform));
    }

    [Fact]
    public async Task GetGamesAsync_FilterBySearch_ReturnsMatching()
    {
        var games = await _service.GetGamesAsync(search: "Zelda");
        Assert.Single(games);
        Assert.Equal("Zelda BOTW", games[0].Title);
    }

    [Fact]
    public async Task GetGamesAsync_FilterByPlatformAndSearch_CombinesFilters()
    {
        var games = await _service.GetGamesAsync(platform: PlatformType.NintendoDS, search: "Pokemon");
        Assert.Single(games);
        Assert.Equal("Pokemon Black", games[0].Title);
    }

    [Fact]
    public async Task GetGamesAsync_SearchNoMatch_ReturnsEmpty()
    {
        var games = await _service.GetGamesAsync(search: "Nonexistent");
        Assert.Empty(games);
    }

    [Fact]
    public async Task GetGamesAsync_HasDlcFlag_SetCorrectly()
    {
        var games = await _service.GetGamesAsync();
        var zelda = games.First(g => g.Title == "Zelda BOTW");
        var mario = games.First(g => g.Title == "Mario Odyssey");

        Assert.True(zelda.HasDlc);
        Assert.False(mario.HasDlc);
    }

    [Fact]
    public async Task GetGamesAsync_HasUpdatesFlag_SetCorrectly()
    {
        var games = await _service.GetGamesAsync();
        var zelda = games.First(g => g.Title == "Zelda BOTW");
        var pokemon = games.First(g => g.Title == "Pokemon Black");

        Assert.True(zelda.HasUpdates);
        Assert.False(pokemon.HasUpdates);
    }

    [Fact]
    public async Task GetGamesAsync_ResultsAreSortedByTitle()
    {
        var games = await _service.GetGamesAsync();
        var titles = games.Select(g => g.Title).ToList();
        Assert.Equal(titles.OrderBy(t => t), titles);
    }

    [Fact]
    public async Task GetGameDetailAsync_ReturnsFullDetail()
    {
        var zelda = _db.Games.First(g => g.Title == "Zelda BOTW");
        var detail = await _service.GetGameDetailAsync(zelda.Id);

        Assert.NotNull(detail);
        Assert.Equal("Zelda BOTW", detail.Title);
        Assert.Equal(PlatformType.NintendoSwitch, detail.Platform);
        Assert.Equal("An adventure game", detail.Description);
        Assert.Equal(14_000_000_000, detail.FileSize);
        Assert.Single(detail.Dlcs);
        Assert.Single(detail.Updates);
        Assert.Equal("DLC Pack 1", detail.Dlcs[0].Title);
        Assert.Equal("1.6.0", detail.Updates[0].Version);
    }

    [Fact]
    public async Task GetGameDetailAsync_NonexistentId_ReturnsNull()
    {
        var detail = await _service.GetGameDetailAsync(9999);
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetPlatformStatsAsync_ReturnsCorrectCounts()
    {
        var stats = await _service.GetPlatformStatsAsync();
        Assert.Equal(3, stats.Count);

        var switchStats = stats.First(s => s.Type == PlatformType.NintendoSwitch);
        Assert.Equal(2, switchStats.GameCount);

        var dsStats = stats.First(s => s.Type == PlatformType.NintendoDS);
        Assert.Equal(1, dsStats.GameCount);
    }

    [Fact]
    public async Task GetEmulatorsAsync_ReturnsAll()
    {
        var emulators = await _service.GetEmulatorsAsync();
        Assert.Equal(3, emulators.Count);
    }

    [Fact]
    public async Task GetEmulatorByPlatformAsync_ReturnsCorrectEmulator()
    {
        var emu = await _service.GetEmulatorByPlatformAsync(PlatformType.NintendoSwitch);
        Assert.NotNull(emu);
        Assert.Equal("Ryubing", emu.Name);
    }

    [Fact]
    public async Task GetGameCountAsync_ReturnsCorrectCount()
    {
        var count = await _service.GetGameCountAsync();
        Assert.Equal(4, count);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class GameAdminServiceTests : IDisposable
{
    private readonly EmulationManagerDbContext _db;
    private readonly GameAdminService _service;

    public GameAdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<EmulationManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new EmulationManagerDbContext(options);
        _service = new GameAdminService(_db);
    }

    [Fact]
    public async Task CreateGameAsync_AddsGameToDb()
    {
        var id = await _service.CreateGameAsync(new GameCreateDto(
            "New Game", PlatformType.NintendoSwitch, "A description", "switch/new.nsp", 1_000_000));

        Assert.True(id > 0);
        Assert.Equal(1, await _db.Games.CountAsync());
    }

    [Fact]
    public async Task UpdateGameAsync_ModifiesExistingGame()
    {
        _db.Games.Add(new GameEntity { Title = "Old Title", Platform = PlatformType.NintendoDS, FilePath = "ds/old.nds" });
        await _db.SaveChangesAsync();
        var game = _db.Games.First();

        var result = await _service.UpdateGameAsync(game.Id, new GameUpdateDto(
            "New Title", PlatformType.NintendoSwitch, null, "switch/new.nsp", 500));

        Assert.True(result);
        var updated = await _db.Games.FindAsync(game.Id);
        Assert.Equal("New Title", updated!.Title);
        Assert.Equal(PlatformType.NintendoSwitch, updated.Platform);
    }

    [Fact]
    public async Task UpdateGameAsync_NonexistentId_ReturnsFalse()
    {
        var result = await _service.UpdateGameAsync(999, new GameUpdateDto("T", PlatformType.NintendoDS, null, "p", 0));
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteGameAsync_RemovesGame()
    {
        _db.Games.Add(new GameEntity { Title = "To Delete", Platform = PlatformType.NintendoDS, FilePath = "ds/delete.nds" });
        await _db.SaveChangesAsync();
        var game = _db.Games.First();

        var result = await _service.DeleteGameAsync(game.Id);

        Assert.True(result);
        Assert.Equal(0, await _db.Games.CountAsync());
    }

    [Fact]
    public async Task DeleteGameAsync_NonexistentId_ReturnsFalse()
    {
        var result = await _service.DeleteGameAsync(999);
        Assert.False(result);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
