using Discord;
using Microsoft.Extensions.Logging;

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
