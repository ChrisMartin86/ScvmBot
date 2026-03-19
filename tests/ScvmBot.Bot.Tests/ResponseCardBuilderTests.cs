using Discord;
using ScvmBot.Bot.Services;

namespace ScvmBot.Bot.Tests;

public class ResponseCardBuilderTests
{
    [Fact]
    public void Build_UsesDefaults_WhenOptionalArgumentsMissing()
    {
        var embed = ResponseCardBuilder.Build("Title", "Description");

        Assert.Equal("Title", embed.Title);
        Assert.Equal("Description", embed.Description);
        Assert.Equal(new Color(88, 101, 242), embed.Color);
        Assert.NotNull(embed.Timestamp);
        Assert.Empty(embed.Fields);
    }

    [Fact]
    public void Build_AddsOnlyNonEmptyFields()
    {
        var fields = new (string Name, string Value, bool Inline)[]
        {
            ("One", "Value", true),
            ("", "Nope", false),
            ("Two", "   ", false),
            ("Three", "Value 3", false)
        };

        var embed = ResponseCardBuilder.Build("Title", "Description", new Color(255, 0, 0), fields);

        Assert.Equal(2, embed.Fields.Length);
        Assert.Equal("One", embed.Fields[0].Name);
        Assert.Equal("Value", embed.Fields[0].Value);
        Assert.True(embed.Fields[0].Inline);
        Assert.Equal("Three", embed.Fields[1].Name);
        Assert.Equal("Value 3", embed.Fields[1].Value);
        Assert.False(embed.Fields[1].Inline);
        Assert.Equal(new Color(255, 0, 0), embed.Color);
    }
}
