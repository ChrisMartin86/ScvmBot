using Discord;
using ScvmBot.Bot.Models;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>Builds a Discord embed for displaying a party of adventurers.</summary>
public static class PartyEmbedBuilder
{
    private static readonly Color PartyColor = new(150, 20, 20); // Dark red for party

    /// <summary>Builds a party sheet embed showing the party name, size, and roster.</summary>
    public static Embed Build(string partyName, IReadOnlyList<ICharacter> members)
    {
        var memberList = string.Join("\n", members.Select(m => $"• {m.Name}"));
        var description = $"Party of {members.Count}\n\n{memberList}";

        var embed = new EmbedBuilder()
            .WithTitle(partyName)
            .WithDescription(description)
            .WithColor(PartyColor)
            .WithFooter("MÖRK BORG is © Ockult Örtmästare Games & Stockholm Kartell. Used under the MÖRK BORG Third Party License.");

        return embed.Build();
    }
}
