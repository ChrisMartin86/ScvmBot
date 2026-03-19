using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;

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

    [ExcludeFromCodeCoverage(Justification = "Accepts sealed SocketSlashCommand; trivial one-liner.")]
    public Task HandleAsync(SocketSlashCommand command)
    {
        return command.RespondAsync($"Hello, {command.User.Mention}! I'm ScvmBot!");
    }
}
