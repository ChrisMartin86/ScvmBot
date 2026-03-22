using Discord;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Abstraction over the Discord interaction context that slash command handlers receive.
/// Replacing the sealed <see cref="Discord.WebSocket.SocketSlashCommand"/> dependency with this
/// interface makes every <see cref="ISlashCommand.HandleAsync"/> implementation independently
/// unit-testable — no live Discord connection or reflection tricks required.
/// </summary>
public interface ISlashCommandContext
{
    /// <summary>Guild the interaction originated from, or <c>null</c> when it is a direct message.</summary>
    ulong? GuildId { get; }

    /// <summary>Snowflake ID of the user who invoked the command.</summary>
    ulong UserId { get; }

    /// <summary>Discord mention string for the invoking user (e.g. <c>&lt;@123456&gt;</c>).</summary>
    string UserMention { get; }

    /// <summary>Top-level options attached to this interaction.</summary>
    IReadOnlyCollection<IApplicationCommandInteractionDataOption> Options { get; }

    /// <summary>The channel in which the interaction was initiated.</summary>
    IMessageChannel Channel { get; }

    /// <summary>Defers the interaction response, keeping the interaction token alive for a follow-up.</summary>
    Task DeferAsync(bool ephemeral = false);

    /// <summary>Sends a follow-up message after a prior <see cref="DeferAsync"/>.</summary>
    Task FollowupAsync(string? text = null, Embed? embed = null, bool ephemeral = false);

    /// <summary>Sends an immediate acknowledgement response to the interaction.</summary>
    Task RespondAsync(string text);

    /// <summary>Opens (or retrieves) the invoking user's direct-message channel.</summary>
    Task<IMessageChannel> CreateUserDMChannelAsync();
}
