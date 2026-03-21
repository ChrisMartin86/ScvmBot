using Discord;
using ScvmBot.Modules;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Converts a transport-agnostic <see cref="CardOutput"/> into a Discord <see cref="Embed"/>.
/// </summary>
internal static class DiscordCardAdapter
{
    public static Embed ToEmbed(CardOutput card)
    {
        var builder = new EmbedBuilder();

        if (card.Title is not null) builder.WithTitle(card.Title);
        if (card.Description is not null) builder.WithDescription(card.Description);
        if (card.Color is not null) builder.WithColor(new Color(card.Color.R, card.Color.G, card.Color.B));
        if (card.Footer is not null) builder.WithFooter(card.Footer);

        if (card.Fields is not null)
            foreach (var field in card.Fields)
                builder.AddField(field.Name, field.Value, field.Inline);

        return builder.Build();
    }
}
