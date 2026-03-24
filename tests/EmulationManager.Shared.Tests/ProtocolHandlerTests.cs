using EmulationManager.Desktop.Services;

namespace EmulationManager.Shared.Tests;

public class ProtocolHandlerTests
{
    private readonly ProtocolHandler _handler = new();

    [Theory]
    [InlineData("emumgr://launch/1", 1)]
    [InlineData("emumgr://launch/42", 42)]
    [InlineData("emumgr://launch/999", 999)]
    public void ParseLaunchGameId_ValidUri_ReturnsGameId(string uri, int expectedId)
    {
        var result = _handler.ParseLaunchGameId(uri);
        Assert.Equal(expectedId, result);
    }

    [Theory]
    [InlineData("emumgr://launch/")]
    [InlineData("emumgr://launch/abc")]
    [InlineData("emumgr://other/1")]
    [InlineData("http://example.com")]
    [InlineData("")]
    [InlineData("emumgr://")]
    public void ParseLaunchGameId_InvalidUri_ReturnsNull(string uri)
    {
        var result = _handler.ParseLaunchGameId(uri);
        Assert.Null(result);
    }

    [Fact]
    public void ParseLaunchGameId_CaseInsensitive()
    {
        var result = _handler.ParseLaunchGameId("EMUMGR://LAUNCH/5");
        Assert.Equal(5, result);
    }

    [Fact]
    public void ParseLaunchGameId_TrailingSlash()
    {
        var result = _handler.ParseLaunchGameId("emumgr://launch/10/");
        Assert.Equal(10, result);
    }
}
