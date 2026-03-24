using Grimoire.Desktop.Services;

namespace Grimoire.Shared.Tests;

public class ProtocolHandlerTests
{
    private readonly ProtocolHandler _handler = new();

    [Theory]
    [InlineData("grimoire://launch/1", 1)]
    [InlineData("grimoire://launch/42", 42)]
    [InlineData("grimoire://launch/999", 999)]
    public void ParseLaunchGameId_ValidUri_ReturnsGameId(string uri, int expectedId)
    {
        var result = _handler.ParseLaunchGameId(uri);
        Assert.Equal(expectedId, result);
    }

    [Theory]
    [InlineData("grimoire://launch/")]
    [InlineData("grimoire://launch/abc")]
    [InlineData("grimoire://other/1")]
    [InlineData("http://example.com")]
    [InlineData("")]
    [InlineData("grimoire://")]
    public void ParseLaunchGameId_InvalidUri_ReturnsNull(string uri)
    {
        var result = _handler.ParseLaunchGameId(uri);
        Assert.Null(result);
    }

    [Fact]
    public void ParseLaunchGameId_CaseInsensitive()
    {
        var result = _handler.ParseLaunchGameId("GRIMOIRE://LAUNCH/5");
        Assert.Equal(5, result);
    }

    [Fact]
    public void ParseLaunchGameId_TrailingSlash()
    {
        var result = _handler.ParseLaunchGameId("grimoire://launch/10/");
        Assert.Equal(10, result);
    }
}
