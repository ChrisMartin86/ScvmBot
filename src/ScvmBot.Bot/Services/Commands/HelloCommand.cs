using Discord;

namespace ScvmBot.Bot.Services.Commands;

/// <summary>The /hello slash command.</summary>
public sealed class HelloCommand : ISlashCommand
{
    public string Name => "hello";

    public SlashCommandBuilder BuildCommand() =>
        new SlashCommandBuilder()
            .WithName("hello")
            .WithDescription("Say hello to ScvmBot!")
            .WithContextTypes(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel);

    public Task HandleAsync(ISlashCommandContext context, CancellationToken ct = default) =>
        context.RespondAsync(
            $"Hello, {context.UserMention}! I'm ScvmBot, a Discord bot for tabletop RPG character generation.\n\nUse **/generate** to create characters. MÖRK BORG is supported.",
            ephemeral: true);
}
