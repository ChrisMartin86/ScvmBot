using Discord;

namespace ScvmBot.Bot.Services;

/// <summary>Builds simple Discord embed cards for status/error messages.</summary>
public static class ResponseCardBuilder
{
    public static Embed Build(
        string title,
        string description,
        Color? color = null,
        IReadOnlyCollection<(string Name, string Value, bool Inline)>? fields = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color ?? new Color(88, 101, 242))
            .WithCurrentTimestamp();

        if (fields != null)
        {
            foreach (var (name, value, inline) in fields)
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                    continue;

                embed.AddField(name, value, inline);
            }
        }

        return embed.Build();
    }
}
