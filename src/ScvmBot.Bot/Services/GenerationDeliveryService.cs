using Discord;
using Microsoft.Extensions.Logging;
using ScvmBot.Modules;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Handles the Discord transport layer for generation results: resolves the target
/// channel (DM vs guild interaction), sends the embed with an optional file attachment,
/// and sends the followup acknowledgement text.
/// <para>
/// The DM send and the followup acknowledgement are separate operations so callers
/// can distinguish "DM failed" from "DM succeeded but acknowledgement failed".
/// </para>
/// </summary>
public class GenerationDeliveryService(ILogger<GenerationDeliveryService> logger)
{
    /// <summary>
    /// Sends the generation result and posts a followup acknowledgement.
    /// Handles DM privacy failures and unexpected send exceptions inline,
    /// posting appropriate error followups so the handler never needs to
    /// reason about transport-layer outcomes.
    /// </summary>
    public async Task DeliverAndAcknowledgeAsync(
        ISlashCommandContext context,
        GenerateResult result,
        Embed embed,
        FileAttachment? attachment,
        CancellationToken ct = default)
    {
        bool sent;
        try
        {
            sent = await SendResultAsync(context, embed, attachment, ct);
        }
        catch (Exception sendEx)
        {
            logger.LogError(sendEx, "Failed to deliver generation result via DM");
            await context.FollowupAsync(
                embed: ResponseCardBuilder.Build("Send Failed",
                    "Something went wrong sending your result. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
            return;
        }

        if (!sent)
        {
            await context.FollowupAsync(
                text: "I couldn't send you a DM. Please enable DMs from server members and try again.",
                ephemeral: true);
            return;
        }

        // Result was delivered successfully. The followup acknowledgement is
        // best-effort — if it fails, the user already has their result.
        try
        {
            var followupText = context.GuildId is null
                ? (result.CharacterCount > 1 ? "Here are your characters!" : "Here's your character!")
                : "Check your DMs.";
            await context.FollowupAsync(text: followupText, ephemeral: true);
        }
        catch (Exception ackEx)
        {
            logger.LogWarning(ackEx,
                "Result delivered successfully but followup acknowledgement failed");
        }
    }

    /// <summary>
    /// Sends the generation result to the user's DM (or the interaction channel if
    /// already in a DM). Returns <c>true</c> if the message was sent successfully,
    /// <c>false</c> if the user's DM privacy settings blocked delivery.
    /// Other failures propagate as exceptions.
    /// </summary>
    public async Task<bool> SendResultAsync(
        ISlashCommandContext context,
        Embed embed,
        FileAttachment? attachment,
        CancellationToken ct = default)
    {
        try
        {
            var isDm = context.GuildId is null;
            IMessageChannel targetChannel = isDm
                ? context.Channel
                : await context.CreateUserDMChannelAsync();

            if (attachment is not null)
                await targetChannel.SendFileAsync(attachment.Value, embed: embed);
            else
                await targetChannel.SendMessageAsync(embed: embed);

            return true;
        }
        catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            logger.LogWarning(httpEx, "Cannot DM user {UserId}", context.UserId);
            return false;
        }
    }
}
