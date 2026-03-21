using Discord;
using Microsoft.Extensions.Logging;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Handles the Discord transport layer for generation results: resolves the target
/// channel (DM vs guild interaction), sends the embed with an optional file attachment,
/// and sends the followup acknowledgement text.
/// The <see cref="Discord.Net.HttpException"/> CannotSendMessageToUser case is handled
/// here so every caller gets uniform behaviour without duplicating the catch logic.
/// Other exceptions propagate to the caller.
/// </summary>
public class GenerationDeliveryService(ILogger<GenerationDeliveryService> logger)
{
    public async Task DeliverAsync(
        ISlashCommandContext context,
        Embed embed,
        FileAttachment? attachment,
        string followupText)
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

            await context.FollowupAsync(text: followupText, ephemeral: true);
        }
        catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            logger.LogWarning(httpEx, "Cannot DM user {UserId}", context.UserId);
            await context.FollowupAsync(
                text: "I couldn't send you a DM. Please enable DMs from server members and try again.",
                ephemeral: true);
        }
    }
}
