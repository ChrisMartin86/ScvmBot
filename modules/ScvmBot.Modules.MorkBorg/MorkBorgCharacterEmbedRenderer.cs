using Discord;
using ScvmBot.Games.MorkBorg.Models;
using System.Text;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Renders a MÖRK BORG <see cref="CharacterGenerationResult"/> as a Discord embed
/// showing abilities, equipment, descriptions, vignette, and scrolls.
/// </summary>
public sealed class MorkBorgCharacterEmbedRenderer : IResultRenderer
{
    private static readonly Color CardColor = new(200, 170, 50);

    public OutputFormat Format => OutputFormat.DiscordEmbed;

    public bool CanRender(GenerateResult result) =>
        result is CharacterGenerationResult { Character: Character };

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not CharacterGenerationResult { Character: Character character })
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a MÖRK BORG character embed.");

        return new EmbedOutput(BuildEmbed(character));
    }

    internal static Embed BuildEmbed(Character character)
    {
        var className = string.IsNullOrWhiteSpace(character.ClassName) ? "No Class" : character.ClassName;
        var summary = $"{className} — HP {character.HitPoints} | Omens {character.Omens} | {character.Silver}s";

        var embed = new EmbedBuilder()
            .WithTitle(character.Name)
            .WithDescription(summary)
            .WithColor(CardColor);

        embed.AddField("Abilities", FormatAbilities(character), inline: false);
        embed.AddField("Equipment", FormatEquipment(character), inline: false);

        var descriptionText = FormatDescriptions(character);
        if (!string.IsNullOrEmpty(descriptionText))
            embed.AddField("Description", descriptionText, inline: false);

        if (!string.IsNullOrWhiteSpace(character.Vignette))
            embed.AddField("Vignette", character.Vignette, inline: false);

        if (character.ScrollsKnown.Count > 0)
            embed.AddField("Scrolls", string.Join("\n", character.ScrollsKnown), inline: false);

        embed.WithFooter("MÖRK BORG is © Ockult Örtmästare Games & Stockholm Kartell. Used under the MÖRK BORG Third Party License.");

        return embed.Build();
    }

    private static string FormatAbilities(Character character) =>
        $"STR {FormatModifier(character.Strength)} · AGI {FormatModifier(character.Agility)} · PRE {FormatModifier(character.Presence)} · TGH {FormatModifier(character.Toughness)}";

    private static string FormatModifier(int value) =>
        value > 0 ? $"+{value}" : value.ToString();

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
            if (desc.Category is DescriptionCategory.Trait
                or DescriptionCategory.Body
                or DescriptionCategory.Habit)
            {
                lines.Add($"{desc.Category}: {desc.Text}");
            }
        }
        return string.Join("\n", lines);
    }
}
