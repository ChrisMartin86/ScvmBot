using Discord;
using Discord.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;
using System.Net;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Direct tests for <see cref="GenerationDeliveryService"/> contract:
/// - DM context uses context.Channel directly
/// - Guild context creates a DM channel
/// - Attachment present → SendFileAsync; absent → SendMessageAsync
/// - CannotSendMessageToUser → returns false
/// - Other HttpException → rethrows
/// </summary>
public class GenerationDeliveryServiceTests
{
    private static GenerationDeliveryService CreateService() =>
        new(NullLogger<GenerationDeliveryService>.Instance);

    private static Embed CreateDummyEmbed() =>
        new EmbedBuilder().WithTitle("Test").WithDescription("Test embed").Build();

    // ── DM context: uses Channel directly, no DM channel creation ────────

    [Fact]
    public async Task SendResultAsync_InDm_UsesContextChannel_WithoutCreatingDmChannel()
    {
        var service = CreateService();
        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null, // DM
            Channel = channel
        };

        var result = await service.SendResultAsync(context, CreateDummyEmbed(), attachment: null);

        Assert.True(result);
        Assert.Equal(1, channel.SendMessageCallCount);
        Assert.Equal(0, context.DmChannelCreationCount);
    }

    // ── Guild context: creates DM channel ────────────────────────────────

    [Fact]
    public async Task SendResultAsync_InGuild_CreatesDmChannel()
    {
        var service = CreateService();
        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = 123456, // Guild
            Channel = channel
        };

        var result = await service.SendResultAsync(context, CreateDummyEmbed(), attachment: null);

        Assert.True(result);
        Assert.Equal(1, context.DmChannelCreationCount);
        Assert.Equal(1, channel.SendMessageCallCount);
    }

    // ── No attachment: SendMessageAsync ──────────────────────────────────

    [Fact]
    public async Task SendResultAsync_WithoutAttachment_CallsSendMessage()
    {
        var service = CreateService();
        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel
        };

        await service.SendResultAsync(context, CreateDummyEmbed(), attachment: null);

        Assert.Equal(1, channel.SendMessageCallCount);
        Assert.Equal(0, channel.SendFileCallCount);
    }

    // ── With attachment: SendFileAsync ───────────────────────────────────

    [Fact]
    public async Task SendResultAsync_WithAttachment_CallsSendFile()
    {
        var service = CreateService();
        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel
        };

        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var attachment = new FileAttachment(stream, "test.pdf");

        await service.SendResultAsync(context, CreateDummyEmbed(), attachment);

        Assert.Equal(0, channel.SendMessageCallCount);
        Assert.Equal(1, channel.SendFileCallCount);
        Assert.Single(channel.SentFileNames, "test.pdf");
    }

    // ── CannotSendMessageToUser returns false ────────────────────────────

    [Fact]
    public async Task SendResultAsync_ReturnsFalse_WhenDmBlocked()
    {
        var service = CreateService();
        var channel = new FakeMessageChannel
        {
            SendException = new HttpException(
                HttpStatusCode.Forbidden,
                request: null!,
                DiscordErrorCode.CannotSendMessageToUser)
        };
        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel
        };

        var result = await service.SendResultAsync(context, CreateDummyEmbed(), attachment: null);

        Assert.False(result);
    }

    // ── Other HttpException propagates ───────────────────────────────────

    [Fact]
    public async Task SendResultAsync_Throws_WhenOtherHttpExceptionOccurs()
    {
        var service = CreateService();
        var channel = new FakeMessageChannel
        {
            SendException = new HttpException(
                HttpStatusCode.InternalServerError,
                request: null!,
                (DiscordErrorCode)0)
        };
        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel
        };

        await Assert.ThrowsAsync<HttpException>(
            () => service.SendResultAsync(context, CreateDummyEmbed(), attachment: null));
    }

    // ── Embed is passed through to the channel ──────────────────────────

    [Fact]
    public async Task SendResultAsync_PassesEmbedToChannel()
    {
        var service = CreateService();
        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel
        };
        var embed = CreateDummyEmbed();

        await service.SendResultAsync(context, embed, attachment: null);

        Assert.Single(channel.SentEmbeds);
        Assert.Equal("Test", channel.SentEmbeds[0]!.Title);
    }
}
