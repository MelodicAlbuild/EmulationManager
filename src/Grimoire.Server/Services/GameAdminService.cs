using Grimoire.Server.Data;
using Grimoire.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grimoire.Server.Services;

public class GameAdminService : IGameAdminService
{
    private readonly GrimoireDbContext _db;

    public GameAdminService(GrimoireDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateGameAsync(GameCreateDto dto)
    {
        var entity = new GameEntity
        {
            Title = dto.Title,
            Platform = dto.Platform,
            Description = dto.Description,
            FilePath = dto.FilePath,
            FileSize = dto.FileSize
        };

        _db.Games.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateGameAsync(int id, GameUpdateDto dto)
    {
        var entity = await _db.Games.FindAsync(id);
        if (entity is null) return false;

        entity.Title = dto.Title;
        entity.Platform = dto.Platform;
        entity.Description = dto.Description;
        entity.FilePath = dto.FilePath;
        entity.FileSize = dto.FileSize;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGameAsync(int id)
    {
        var entity = await _db.Games.FindAsync(id);
        if (entity is null) return false;

        _db.Games.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
