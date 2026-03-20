using Discord;

namespace ScvmBot.Rendering;

/// <summary>
/// Single entry point the bot uses to host a game system.
/// Each module owns its command definitions, generation handling, and renderer
/// registration.  The bot depends on this contract — never on a concrete game package.
/// </summary>
public interface IGameModule
{
    string Name { get; }
    string CommandKey { get; }
    SlashCommandOptionBuilder BuildCommandGroupOptions();
    Task<GenerateResult> HandleGenerateCommandAsync(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions,
        CancellationToken ct = default);
}
