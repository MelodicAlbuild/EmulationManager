using Grimoire.Server.Entities;
using Grimoire.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grimoire.Server.Data;

public static class SeedDataService
{
    public static async Task SeedAsync(GrimoireDbContext db)
    {
        if (await db.Games.AnyAsync())
            return;

        // Emulators
        var ryubing = new EmulatorEntity
        {
            Name = "Ryubing",
            Platform = PlatformType.NintendoSwitch,
            Version = "1.2.82",
            DownloadUrl = "https://github.com/Ryubing/Ryujinx/releases",
            ExecutableName = "Ryujinx.exe"
        };
        var melonds = new EmulatorEntity
        {
            Name = "melonDS",
            Platform = PlatformType.NintendoDS,
            Version = "1.0.0",
            DownloadUrl = "https://github.com/melonDS-emu/melonDS/releases",
            ExecutableName = "melonDS.exe"
        };
        var citra = new EmulatorEntity
        {
            Name = "Citra",
            Platform = PlatformType.Nintendo3DS,
            Version = "2104",
            DownloadUrl = "https://github.com/PabloMK7/citra/releases",
            ExecutableName = "citra-qt.exe"
        };
        db.Emulators.AddRange(ryubing, melonds, citra);

        // Switch games
        var botw = new GameEntity
        {
            Title = "The Legend of Zelda: Breath of the Wild",
            Platform = PlatformType.NintendoSwitch,
            Description = "Step into a world of discovery, exploration, and adventure in The Legend of Zelda: Breath of the Wild.",
            FilePath = "switch/zelda_botw.nsp",
            FileSize = 14_400_000_000L,
            Dlcs =
            [
                new DlcEntity { Title = "The Master Trials", FilePath = "switch/dlc/zelda_botw_dlc1.nsp", FileSize = 456_000_000L },
                new DlcEntity { Title = "The Champions' Ballad", FilePath = "switch/dlc/zelda_botw_dlc2.nsp", FileSize = 2_100_000_000L }
            ],
            Updates =
            [
                new UpdateEntity { Title = "v1.6.0", Version = "1.6.0", FilePath = "switch/updates/zelda_botw_v1.6.0.nsp", FileSize = 562_000_000L }
            ]
        };

        var mario = new GameEntity
        {
            Title = "Super Mario Odyssey",
            Platform = PlatformType.NintendoSwitch,
            Description = "Join Mario on a massive, globe-trotting 3D adventure.",
            FilePath = "switch/mario_odyssey.nsp",
            FileSize = 5_700_000_000L,
            Updates =
            [
                new UpdateEntity { Title = "v1.3.0", Version = "1.3.0", FilePath = "switch/updates/mario_odyssey_v1.3.0.nsp", FileSize = 200_000_000L }
            ]
        };

        var metroid = new GameEntity
        {
            Title = "Metroid Dread",
            Platform = PlatformType.NintendoSwitch,
            Description = "Join bounty hunter Samus Aran as she escapes a deadly alien world.",
            FilePath = "switch/metroid_dread.nsp",
            FileSize = 4_700_000_000L
        };

        var splatoon = new GameEntity
        {
            Title = "Splatoon 3",
            Platform = PlatformType.NintendoSwitch,
            Description = "Enter the Splatlands, a sun-scorched desert inhabited by battle-hardened Inklings and Octolings.",
            FilePath = "switch/splatoon3.nsp",
            FileSize = 6_000_000_000L,
            Dlcs =
            [
                new DlcEntity { Title = "Expansion Pass - Side Order", FilePath = "switch/dlc/splatoon3_side_order.nsp", FileSize = 1_800_000_000L }
            ],
            Updates =
            [
                new UpdateEntity { Title = "v7.0.0", Version = "7.0.0", FilePath = "switch/updates/splatoon3_v7.0.0.nsp", FileSize = 1_500_000_000L }
            ]
        };

        // DS games
        var pokemonBlack = new GameEntity
        {
            Title = "Pokemon Black",
            Platform = PlatformType.NintendoDS,
            Description = "Begin your adventure in the Unova region with all new Pokemon.",
            FilePath = "ds/pokemon_black.nds",
            FileSize = 128_000_000L
        };

        var marioKart = new GameEntity
        {
            Title = "Mario Kart DS",
            Platform = PlatformType.NintendoDS,
            Description = "Race your friends across classic and new tracks.",
            FilePath = "ds/mario_kart_ds.nds",
            FileSize = 32_000_000L
        };

        var phoenix = new GameEntity
        {
            Title = "Phoenix Wright: Ace Attorney",
            Platform = PlatformType.NintendoDS,
            Description = "Step into the shoes of a rookie defense attorney and fight for justice.",
            FilePath = "ds/phoenix_wright.nds",
            FileSize = 64_000_000L
        };

        // 3DS games
        var pokemon3ds = new GameEntity
        {
            Title = "Pokemon Ultra Sun",
            Platform = PlatformType.Nintendo3DS,
            Description = "A new light shines on the Alola region in Pokemon Ultra Sun.",
            FilePath = "3ds/pokemon_ultra_sun.3ds",
            FileSize = 3_600_000_000L,
            Dlcs =
            [
                new DlcEntity { Title = "Special Demo Version Save Bonus", Version = "1.0", FilePath = "3ds/dlc/pokemon_us_bonus.cia", FileSize = 10_000_000L }
            ]
        };

        var fireEmblem = new GameEntity
        {
            Title = "Fire Emblem Awakening",
            Platform = PlatformType.Nintendo3DS,
            Description = "Strategize your way through battles in this beloved tactical RPG.",
            FilePath = "3ds/fire_emblem_awakening.3ds",
            FileSize = 1_800_000_000L
        };

        var zelda3ds = new GameEntity
        {
            Title = "The Legend of Zelda: A Link Between Worlds",
            Platform = PlatformType.Nintendo3DS,
            Description = "Two worlds collide in this reimagining of the classic SNES adventure.",
            FilePath = "3ds/zelda_albw.3ds",
            FileSize = 512_000_000L
        };

        db.Games.AddRange(botw, mario, metroid, splatoon, pokemonBlack, marioKart, phoenix, pokemon3ds, fireEmblem, zelda3ds);
        await db.SaveChangesAsync();
    }
}
