using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Thin adapter that exposes a <see cref="SocketSlashCommand"/> as <see cref="ISlashCommandContext"/>.
/// Every member delegates directly to the underlying Discord socket object; there is no logic here
/// to unit-test, so the class is excluded from code coverage.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin adapter over the sealed SocketSlashCommand type; no testable logic.")]
internal sealed class SocketSlashCommandContext : ISlashCommandContext
{
    private readonly SocketSlashCommand _command;

    internal SocketSlashCommandContext(SocketSlashCommand command) => _command = command;

    public ulong? GuildId => _command.GuildId;
    public ulong UserId => _command.User.Id;
    public string UserMention => _command.User.Mention;
    public IReadOnlyCollection<IApplicationCommandInteractionDataOption> Options => _command.Data.Options;
    public IMessageChannel Channel => _command.Channel;

    public Task DeferAsync(bool ephemeral = false) =>
        _command.DeferAsync(ephemeral: ephemeral);

    public Task FollowupAsync(string? text = null, Embed? embed = null, bool ephemeral = false) =>
        _command.FollowupAsync(text: text, embed: embed, ephemeral: ephemeral);

    public Task RespondAsync(string text) =>
        _command.RespondAsync(text: text);

    public async Task<IMessageChannel> CreateUserDMChannelAsync() =>
        await _command.User.CreateDMChannelAsync();
}
