using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class MorkBorgGenerateOptionParserTests
{
    [Fact]
    public void Parse_ShouldReturnDefaults_WhenOptionsEmpty()
    {
        var result = MorkBorgGenerateOptionParser.Parse(
            new Dictionary<string, object?>());

        Assert.NotNull(result);
        Assert.Equal(AbilityRollMethod.ThreeD6, result.RollMethod);
        Assert.Null(result.ClassName);
        Assert.Null(result.Name);
    }

    [Fact]
    public void Parse_ShouldParse_RollMethodOption()
    {
        var result = MorkBorgGenerateOptionParser.Parse(
            new Dictionary<string, object?> { ["roll-method"] = MorkBorgCommandDefinition.ChoiceFourD6Drop });

        Assert.Equal(AbilityRollMethod.FourD6DropLowest, result.RollMethod);
    }

    [Fact]
    public void Parse_ShouldParse_ClassOption()
    {
        var result = MorkBorgGenerateOptionParser.Parse(
            new Dictionary<string, object?> { ["class"] = "Fanged Deserter" });

        Assert.Equal("Fanged Deserter", result.ClassName);
    }

    [Fact]
    public void Parse_ShouldPreserve_NoneClassAs_None()
    {
        var result = MorkBorgGenerateOptionParser.Parse(
            new Dictionary<string, object?> { ["class"] = MorkBorgCommandDefinition.ChoiceClassNone });

        Assert.Equal("none", result.ClassName);
    }

    [Fact]
    public void Parse_ShouldReturn_NullClassName_WhenClassIsOmitted()
    {
        var result = MorkBorgGenerateOptionParser.Parse(
            new Dictionary<string, object?>
            {
                ["roll-method"] = MorkBorgCommandDefinition.Choice3D6,
                ["name"] = "Test Character"
            });

        Assert.Null(result.ClassName);
    }

    [Fact]
    public void Parse_ShouldParse_AllOptionsSimultaneously()
    {
        var result = MorkBorgGenerateOptionParser.Parse(
            new Dictionary<string, object?>
            {
                ["roll-method"] = MorkBorgCommandDefinition.ChoiceFourD6Drop,
                ["class"] = "Wretched Royalty",
                ["name"] = "Gertrude"
            });

        Assert.Equal(AbilityRollMethod.FourD6DropLowest, result.RollMethod);
        Assert.Equal("Wretched Royalty", result.ClassName);
        Assert.Equal("Gertrude", result.Name);
    }

    // ── ParseCount ──────────────────────────────────────────────────────────

    [Fact]
    public void ParseCount_ReturnsOne_WhenCountNotSpecified()
    {
        var count = MorkBorgGenerateOptionParser.ParseCount(
            new Dictionary<string, object?>());

        Assert.Equal(1, count);
    }

    [Fact]
    public void ParseCount_ReturnsOne_WhenCountIsNull()
    {
        var count = MorkBorgGenerateOptionParser.ParseCount(
            new Dictionary<string, object?> { ["count"] = null });

        Assert.Equal(1, count);
    }

    [Fact]
    public void ParseCount_ReturnsExplicitValue_WhenProvided()
    {
        var count = MorkBorgGenerateOptionParser.ParseCount(
            new Dictionary<string, object?> { ["count"] = 3L });

        Assert.Equal(3, count);
    }

    [Fact]
    public void ParseCount_AcceptsOne()
    {
        var count = MorkBorgGenerateOptionParser.ParseCount(
            new Dictionary<string, object?> { ["count"] = 1L });

        Assert.Equal(1, count);
    }

    [Fact]
    public void ParseCount_ThrowsArgumentException_WhenZero()
    {
        Assert.Throws<ArgumentException>(() =>
            MorkBorgGenerateOptionParser.ParseCount(
                new Dictionary<string, object?> { ["count"] = 0L }));
    }

    [Fact]
    public void ParseCount_ThrowsArgumentException_WhenNegative()
    {
        Assert.Throws<ArgumentException>(() =>
            MorkBorgGenerateOptionParser.ParseCount(
                new Dictionary<string, object?> { ["count"] = -1L }));
    }

    [Fact]
    public void ParseCount_ThrowsArgumentException_WhenNotNumeric()
    {
        Assert.Throws<ArgumentException>(() =>
            MorkBorgGenerateOptionParser.ParseCount(
                new Dictionary<string, object?> { ["count"] = "nope" }));
    }

    [Fact]
    public void ParseCount_ThrowsArgumentException_WhenNotConvertible()
    {
        Assert.Throws<ArgumentException>(() =>
            MorkBorgGenerateOptionParser.ParseCount(
                new Dictionary<string, object?> { ["count"] = new object() }));
    }
}
