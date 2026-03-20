namespace ScvmBot.Bot.Tests;

internal static class TestInfrastructure
{
    public static string GetRepositoryRoot() => SharedTestInfrastructure.GetRepositoryRoot();
    public static string GetBotProjectPath() => SharedTestInfrastructure.GetBotProjectPath();
    public static string CreateTempDirectory() => SharedTestInfrastructure.CreateTempDirectory();
}

/// <summary>
/// Test double for <see cref="ScvmBot.Bot.Services.ISlashCommandContext"/>.
/// Captures all interactions so tests can assert on them.
/// </summary>
internal sealed class FakeCommandContext : ScvmBot.Bot.Services.ISlashCommandContext
{
    public ulong? GuildId { get; set; }
    public ulong UserId { get; set; } = 1;
    public string UserMention { get; set; } = "@User";
    public IReadOnlyCollection<Discord.IApplicationCommandInteractionDataOption> Options { get; set; }
        = Array.Empty<Discord.IApplicationCommandInteractionDataOption>();
    public Discord.IMessageChannel Channel { get; set; } = null!;

    public bool Deferred { get; private set; }
    public List<string?> FollowupTexts { get; } = new();
    public List<Discord.Embed?> FollowupEmbeds { get; } = new();
    public List<string> RespondTexts { get; } = new();

    public Task DeferAsync(bool ephemeral = false) { Deferred = true; return Task.CompletedTask; }

    public Task FollowupAsync(string? text = null, Discord.Embed? embed = null, bool ephemeral = false)
    {
        FollowupTexts.Add(text);
        FollowupEmbeds.Add(embed);
        return Task.CompletedTask;
    }

    public Task RespondAsync(string text) { RespondTexts.Add(text); return Task.CompletedTask; }

    public Task<Discord.IMessageChannel> CreateUserDMChannelAsync() =>
        Task.FromResult(Channel);
}

/// <summary>
/// Test double for <see cref="Discord.IMessageChannel"/>.
/// Only <see cref="SendMessageAsync"/> and <see cref="SendFileAsync(Discord.FileAttachment, string?, bool, Discord.Embed?, Discord.RequestOptions?, Discord.AllowedMentions?, Discord.MessageReference?, Discord.MessageComponent?, Discord.ISticker[]?, Discord.Embed[]?, Discord.MessageFlags)"/>
/// are implemented; all other members throw <see cref="NotImplementedException"/>.
/// </summary>
internal sealed class FakeMessageChannel : Discord.IMessageChannel
{
    public List<Discord.Embed?> SentEmbeds { get; } = new();
    public int SendMessageCallCount { get; private set; }
    public int SendFileCallCount { get; private set; }

    // IEntity<ulong>
    public ulong Id => 1;

    // ISnowflakeEntity
    public DateTimeOffset CreatedAt => DateTimeOffset.UtcNow;

    // IDeletable
    public Task DeleteAsync(Discord.RequestOptions? options = null) => throw new NotImplementedException();

    // IMentionable
    public string Mention => "#fake";

    // IChannel
    public string Name => "fake";
    public Discord.ChannelType ChannelType => Discord.ChannelType.DM;
    public Task<Discord.IUser> GetUserAsync(ulong id, Discord.CacheMode mode = Discord.CacheMode.AllowDownload, Discord.RequestOptions? options = null)
        => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyCollection<Discord.IUser>> GetUsersAsync(Discord.CacheMode mode = Discord.CacheMode.AllowDownload, Discord.RequestOptions? options = null)
        => throw new NotImplementedException();

    // IMessageChannel — implemented
    public Task<Discord.IUserMessage> SendMessageAsync(string? text = null, bool isTTS = false, Discord.Embed? embed = null,
        Discord.RequestOptions? options = null, Discord.AllowedMentions? allowedMentions = null,
        Discord.MessageReference? messageReference = null, Discord.MessageComponent? components = null,
        Discord.ISticker[]? stickers = null, Discord.Embed[]? embeds = null, Discord.MessageFlags flags = Discord.MessageFlags.None,
        Discord.PollProperties? pollProperties = null)
    {
        SendMessageCallCount++;
        SentEmbeds.Add(embed);
        return Task.FromResult<Discord.IUserMessage>(null!);
    }

    public Task<Discord.IUserMessage> SendFileAsync(Discord.FileAttachment attachment, string? text = null, bool isTTS = false,
        Discord.Embed? embed = null, Discord.RequestOptions? options = null, Discord.AllowedMentions? allowedMentions = null,
        Discord.MessageReference? messageReference = null, Discord.MessageComponent? components = null,
        Discord.ISticker[]? stickers = null, Discord.Embed[]? embeds = null, Discord.MessageFlags flags = Discord.MessageFlags.None,
        Discord.PollProperties? pollProperties = null)
    {
        SendFileCallCount++;
        SentEmbeds.Add(embed);
        return Task.FromResult<Discord.IUserMessage>(null!);
    }

    // IMessageChannel — not implemented (unused by production code under test)
    public Task<Discord.IMessage> GetMessageAsync(ulong id, Discord.CacheMode mode = Discord.CacheMode.AllowDownload, Discord.RequestOptions? options = null)
        => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyCollection<Discord.IMessage>> GetMessagesAsync(int limit = 100, Discord.CacheMode mode = Discord.CacheMode.AllowDownload, Discord.RequestOptions? options = null)
        => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyCollection<Discord.IMessage>> GetMessagesAsync(ulong fromMessageId, Discord.Direction dir, int limit = 100, Discord.CacheMode mode = Discord.CacheMode.AllowDownload, Discord.RequestOptions? options = null)
        => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyCollection<Discord.IMessage>> GetMessagesAsync(Discord.IMessage fromMessage, Discord.Direction dir, int limit = 100, Discord.CacheMode mode = Discord.CacheMode.AllowDownload, Discord.RequestOptions? options = null)
        => throw new NotImplementedException();
    public Task<IReadOnlyCollection<Discord.IMessage>> GetPinnedMessagesAsync(Discord.RequestOptions? options = null)
        => throw new NotImplementedException();
    public Task<Discord.IUserMessage> SendFileAsync(string filePath, string? text = null, bool isTTS = false, Discord.Embed? embed = null,
        Discord.RequestOptions? options = null, bool isSpoiler = false, Discord.AllowedMentions? allowedMentions = null,
        Discord.MessageReference? messageReference = null, Discord.MessageComponent? components = null,
        Discord.ISticker[]? stickers = null, Discord.Embed[]? embeds = null, Discord.MessageFlags flags = Discord.MessageFlags.None,
        Discord.PollProperties? pollProperties = null)
        => throw new NotImplementedException();
    public Task<Discord.IUserMessage> SendFileAsync(System.IO.Stream stream, string filename, string? text = null, bool isTTS = false,
        Discord.Embed? embed = null, Discord.RequestOptions? options = null, bool isSpoiler = false,
        Discord.AllowedMentions? allowedMentions = null, Discord.MessageReference? messageReference = null,
        Discord.MessageComponent? components = null, Discord.ISticker[]? stickers = null, Discord.Embed[]? embeds = null,
        Discord.MessageFlags flags = Discord.MessageFlags.None, Discord.PollProperties? pollProperties = null)
        => throw new NotImplementedException();
    public Task<Discord.IUserMessage> SendFilesAsync(IEnumerable<Discord.FileAttachment> attachments, string? text = null, bool isTTS = false,
        Discord.Embed? embed = null, Discord.RequestOptions? options = null, Discord.AllowedMentions? allowedMentions = null,
        Discord.MessageReference? messageReference = null, Discord.MessageComponent? components = null,
        Discord.ISticker[]? stickers = null, Discord.Embed[]? embeds = null, Discord.MessageFlags flags = Discord.MessageFlags.None,
        Discord.PollProperties? pollProperties = null)
        => throw new NotImplementedException();
    public Task<Discord.IUserMessage> ModifyMessageAsync(ulong messageId, Action<Discord.MessageProperties> func, Discord.RequestOptions? options = null)
        => throw new NotImplementedException();
    public Task DeleteMessageAsync(ulong messageId, Discord.RequestOptions? options = null) => throw new NotImplementedException();
    public Task DeleteMessageAsync(Discord.IMessage message, Discord.RequestOptions? options = null) => throw new NotImplementedException();
    public Task TriggerTypingAsync(Discord.RequestOptions? options = null) => throw new NotImplementedException();
    public IDisposable EnterTypingState(Discord.RequestOptions? options = null) => throw new NotImplementedException();
}

