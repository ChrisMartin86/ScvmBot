using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class MorkBorgPartyOptionParserTests
{
    [Fact]
    public void ParsePartySize_ReturnsDefaultSize_WhenNoSizeOption()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(
            new Dictionary<string, object?>());
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ReturnsDefaultSize_WhenSizeIsNull()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(
            new Dictionary<string, object?> { ["size"] = null });
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ParsesLongValue_FromSize()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(
            new Dictionary<string, object?> { ["size"] = 3L });
        Assert.Equal(3, size);
    }

    [Fact]
    public void ParsePartySize_ClampsLow_BelowMinimum()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(
            new Dictionary<string, object?> { ["size"] = 0L });
        Assert.Equal(1, size);
    }

    [Fact]
    public void ParsePartySize_ClampsHigh_AboveMaximum()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(
            new Dictionary<string, object?> { ["size"] = 20L });
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ReturnsDefault_WhenValueIsNonNumericString()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(
            new Dictionary<string, object?> { ["size"] = "three" });
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_AcceptsIntValue()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(
            new Dictionary<string, object?> { ["size"] = 2 });
        Assert.Equal(2, size);
    }
}
