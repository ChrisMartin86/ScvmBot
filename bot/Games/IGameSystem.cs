using Discord;
using Discord.Interactions;

namespace ScvmBot.Bot.Games;

/// <summary>
/// Plugin interface for a game system under the /generate command.
/// Implement this interface and register via DI to add a new system.
/// Keep focused on content generation — Discord infrastructure belongs in services.
/// </summary>
public interface IGameSystem
{
    string Name { get; }
    string CommandKey { get; }
    SlashCommandOptionBuilder BuildCommandGroupOptions();
    Task<GenerateResult> HandleGenerateCommandAsync(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions,
        CancellationToken ct = default);
}
