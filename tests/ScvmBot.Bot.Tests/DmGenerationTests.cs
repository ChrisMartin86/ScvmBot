using Discord;
using ScvmBot.Bot.Services;
using ScvmBot.Bot.Services.Commands;

namespace ScvmBot.Bot.Tests;

public class DmGenerationTests
{
    [Fact]
    public void IsDmInteraction_ReturnsTrue_WhenGuildIdIsNull()
    {
        Assert.True(GenerateCommandHandler.IsDmInteraction(null));
    }

    [Fact]
    public void IsDmInteraction_ReturnsFalse_WhenGuildIdHasValue()
    {
        Assert.False(GenerateCommandHandler.IsDmInteraction(123456789UL));
    }

    [Fact]
    public void GetFollowupText_ReturnsDmText_WhenDm()
    {
        var text = GenerateCommandHandler.GetFollowupText(isDm: true);
        Assert.Equal("Here's your character!", text);
    }

    [Fact]
    public void GetFollowupText_ReturnsGuildText_WhenNotDm()
    {
        var text = GenerateCommandHandler.GetFollowupText(isDm: false);
        Assert.Equal("Check your DMs.", text);
    }

    [Fact]
    public void GetPartyFollowupText_ReturnsDmText_WhenDm()
    {
        var text = GenerateCommandHandler.GetPartyFollowupText(isDm: true);
        Assert.Equal("Here's your party!", text);
    }

    [Fact]
    public void GetPartyFollowupText_ReturnsGuildText_WhenNotDm()
    {
        var text = GenerateCommandHandler.GetPartyFollowupText(isDm: false);
        Assert.Equal("Check your DMs.", text);
    }

    [Fact]
    public void GenerateCommand_HasDmContextTypes()
    {
        var handler = CreateMinimalHandler();
        var builder = handler.BuildCommand();
        Assert.Contains(InteractionContextType.Guild, builder.ContextTypes);
        Assert.Contains(InteractionContextType.BotDm, builder.ContextTypes);
        Assert.Contains(InteractionContextType.PrivateChannel, builder.ContextTypes);
    }

    [Fact]
    public void HelloCommand_HasDmContextTypes()
    {
        var command = new HelloCommand();
        var builder = command.BuildCommand();
        Assert.Contains(InteractionContextType.Guild, builder.ContextTypes);
        Assert.Contains(InteractionContextType.BotDm, builder.ContextTypes);
        Assert.Contains(InteractionContextType.PrivateChannel, builder.ContextTypes);
    }

    private static GenerateCommandHandler CreateMinimalHandler()
    {
        return new GenerateCommandHandler(
            gameSystems: Array.Empty<Games.IGameSystem>(),
            logger: new Microsoft.Extensions.Logging.Abstractions.NullLogger<GenerateCommandHandler>());
    }
}
