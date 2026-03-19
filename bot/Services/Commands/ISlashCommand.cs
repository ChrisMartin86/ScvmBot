using Discord;
using Discord.WebSocket;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Abstraction for a Discord slash command.
/// Commands are discovered and registered via DI, keeping BotService command-agnostic.
/// </summary>
public interface ISlashCommand
{
    string Name { get; }
    SlashCommandBuilder BuildCommand();
    Task HandleAsync(SocketSlashCommand command);
}
