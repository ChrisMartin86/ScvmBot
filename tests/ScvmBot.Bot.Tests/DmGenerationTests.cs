using Discord;
using ScvmBot.Bot.Services;
using ScvmBot.Bot.Services.Commands;
using ScvmBot.Rendering;

namespace ScvmBot.Bot.Tests;

public class DmGenerationTests
{
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

    [Fact]
    public async Task HelloCommand_HandleAsync_RespondsWithUserMention()
    {
        var command = new HelloCommand();
        var context = new FakeCommandContext { UserMention = "<@123>" };

        await command.HandleAsync(context);

        Assert.Single(context.RespondTexts);
        Assert.Contains("<@123>", context.RespondTexts[0]);
    }

    private static GenerateCommandHandler CreateMinimalHandler() =>
        new(gameSystems: Array.Empty<Games.IGameSystem>(),
            rendererRegistry: new RendererRegistry(Array.Empty<IResultRenderer>()),
            delivery: new GenerationDeliveryService(Microsoft.Extensions.Logging.Abstractions.NullLogger<GenerationDeliveryService>.Instance),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<GenerateCommandHandler>.Instance);
}

