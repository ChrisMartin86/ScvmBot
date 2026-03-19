using Discord;
using ScvmBot.Bot.Models.MorkBorg;
using System.Text;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>Builds a rich Discord embed for displaying a generated <see cref="Character"/>.</summary>
public static class CharacterCardBuilder
{
    // MÖRK BORG palette
    private static readonly Color CardColor = new(200, 170, 50);

    /// <summary>Builds a character summary embed with abilities, equipment, and descriptions.</summary>
    public static Embed Build(Character character, AbilityRollMethod rollMethod = AbilityRollMethod.ThreeD6)
    {
        var className = string.IsNullOrWhiteSpace(character.ClassName) ? "No Class" : character.ClassName;
        var summary = $"{className} — HP {character.HitPoints} | Omens {character.Omens} | {character.Silver}s";

        var embed = new EmbedBuilder()
            .WithTitle(character.Name)
            .WithDescription(summary)
            .WithColor(CardColor);

        // Abilities
        embed.AddField("Abilities", FormatAbilities(character), inline: false);

        // Equipment
        embed.AddField("Equipment", FormatEquipment(character), inline: false);

        // Description (Trait / Body / Habit)
        var descriptionText = FormatDescriptions(character);
        if (!string.IsNullOrEmpty(descriptionText))
            embed.AddField("Description", descriptionText, inline: false);

        // Vignette
        if (!string.IsNullOrWhiteSpace(character.Vignette))
            embed.AddField("Vignette", character.Vignette, inline: false);

        // Scrolls
        if (character.ScrollsKnown.Count > 0)
            embed.AddField("Scrolls", string.Join("\n", character.ScrollsKnown), inline: false);

        embed.WithFooter("MÖRK BORG is © Ockult Örtmästare Games & Stockholm Kartell. Used under the MÖRK BORG Third Party License.");

        return embed.Build();
    }

    private static string FormatAbilities(Character character)
    {
        return $"STR {FormatModifier(character.Strength)} · AGI {FormatModifier(character.Agility)} · PRE {FormatModifier(character.Presence)} · TGH {FormatModifier(character.Toughness)}";
    }

    private static string FormatModifier(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private static string FormatEquipment(Character character)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.IsNullOrWhiteSpace(character.EquippedWeapon) ? "No Weapon" : character.EquippedWeapon);
        sb.AppendLine(string.IsNullOrWhiteSpace(character.EquippedArmor) ? "No Armor" : character.EquippedArmor);

        foreach (var item in character.Items)
            sb.AppendLine(item);

        return sb.ToString().TrimEnd();
    }

    private static string FormatDescriptions(Character character)
    {
        var lines = new List<string>();

        foreach (var desc in character.Descriptions)
        {
            if (desc.StartsWith("Trait:", StringComparison.OrdinalIgnoreCase) ||
                desc.StartsWith("Body:", StringComparison.OrdinalIgnoreCase) ||
                desc.StartsWith("Habit:", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(desc);
            }
        }

        return string.Join("\n", lines);
    }
}
