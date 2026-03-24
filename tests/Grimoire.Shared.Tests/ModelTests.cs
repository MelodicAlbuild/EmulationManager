using System.Text.Json;
using System.Text.Json.Serialization;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Models;

namespace Grimoire.Shared.Tests;

public class ModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void GameInfo_Record_Equality()
    {
        var a = new GameInfo(1, "Test", PlatformType.NintendoSwitch, "Desc", null, 1000, null);
        var b = new GameInfo(1, "Test", PlatformType.NintendoSwitch, "Desc", null, 1000, null);
        Assert.Equal(a, b);
    }

    [Fact]
    public void GameListDto_Serialization_RoundTrip()
    {
        var dto = new GameListDto(1, "Zelda", PlatformType.NintendoSwitch, null, true, false);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<GameListDto>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(dto.Id, deserialized.Id);
        Assert.Equal(dto.Title, deserialized.Title);
        Assert.Equal(dto.Platform, deserialized.Platform);
        Assert.Equal(dto.HasDlc, deserialized.HasDlc);
        Assert.False(deserialized.HasUpdates);
    }

    [Fact]
    public void GameDetailDto_Serialization_IncludesDlcAndUpdates()
    {
        var dto = new GameDetailDto(
            1, "Zelda", PlatformType.NintendoSwitch, "A game", null, 14_000_000_000, null,
            [new DlcInfo(1, 1, "DLC Pack 1", "1.0", 500_000_000)],
            [new UpdateInfo(1, 1, "v1.6.0", "1.6.0", 200_000_000)]
        );

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<GameDetailDto>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Dlcs);
        Assert.Single(deserialized.Updates);
        Assert.Equal("DLC Pack 1", deserialized.Dlcs[0].Title);
        Assert.Equal("1.6.0", deserialized.Updates[0].Version);
    }

    [Fact]
    public void DownloadProgressDto_ProgressPercent_CalculatesCorrectly()
    {
        var dto = new DownloadProgressDto("abc", 500, 1000, DownloadStatus.Downloading);
        Assert.Equal(50.0, dto.ProgressPercent);
    }

    [Fact]
    public void DownloadProgressDto_ProgressPercent_ZeroWhenTotalIsZero()
    {
        var dto = new DownloadProgressDto("abc", 0, 0, DownloadStatus.Queued);
        Assert.Equal(0.0, dto.ProgressPercent);
    }

    [Theory]
    [InlineData(PlatformType.NintendoSwitch)]
    [InlineData(PlatformType.NintendoDS)]
    [InlineData(PlatformType.Nintendo3DS)]
    public void PlatformType_Enum_HasExpectedValues(PlatformType platform)
    {
        Assert.True(Enum.IsDefined(platform));
    }

    [Fact]
    public void PlatformType_Enum_HasThreeValues()
    {
        Assert.Equal(3, Enum.GetValues<PlatformType>().Length);
    }

    [Fact]
    public void DownloadableType_Enum_HasAllExpectedValues()
    {
        var values = Enum.GetValues<DownloadableType>();
        Assert.Contains(DownloadableType.Game, values);
        Assert.Contains(DownloadableType.Dlc, values);
        Assert.Contains(DownloadableType.Update, values);
        Assert.Contains(DownloadableType.Emulator, values);
        Assert.Contains(DownloadableType.Firmware, values);
        Assert.Contains(DownloadableType.Bios, values);
    }

    [Fact]
    public void LaunchOptions_DefaultValues()
    {
        var options = new LaunchOptions();
        Assert.True(options.Fullscreen);
        Assert.Null(options.CustomArgs);
    }

    [Fact]
    public void LaunchOptions_CustomValues()
    {
        var options = new LaunchOptions(Fullscreen: false, CustomArgs: "--verbose");
        Assert.False(options.Fullscreen);
        Assert.Equal("--verbose", options.CustomArgs);
    }

    [Fact]
    public void LibraryItem_ContainsGameAndContent()
    {
        var game = new GameInfo(1, "Test", PlatformType.NintendoDS, null, null, 100, null);
        var dlcs = new List<DlcInfo> { new(1, 1, "DLC", null, 50) };
        var updates = new List<UpdateInfo> { new(1, 1, "Update", "1.0", 25) };
        var item = new LibraryItem(game, dlcs, updates);

        Assert.Equal("Test", item.Game.Title);
        Assert.Single(item.Dlcs);
        Assert.Single(item.Updates);
    }

    [Fact]
    public void PlatformInfoDto_Serialization()
    {
        var dto = new PlatformInfoDto(PlatformType.Nintendo3DS, "Nintendo3DS", 5);
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        Assert.Contains("Nintendo3DS", json);
        Assert.Contains("5", json);
    }
}
