using ScvmBot.Games.MorkBorg.Models;
using System.Text;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Renders a MÖRK BORG <see cref="GenerationBatch"/> as a structured card.
/// Single character: detailed card with abilities, equipment, descriptions, vignette, and scrolls.
/// Multiple characters: roster card showing group name and member list.
/// </summary>
public sealed class MorkBorgCharacterEmbedRenderer : IResultRenderer
{
    private static readonly CardColor SingleColor = new(200, 170, 50);
    private static readonly CardColor GroupColor = new(150, 20, 20);

    public Type ResultType => typeof(GenerationBatch<Character>);

    public OutputFormat Format => OutputFormat.Card;

    public bool CanRender(GenerateResult result) =>
        result is GenerationBatch<Character>;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not GenerationBatch<Character> charResult)
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a MÖRK BORG character card.");

        return charResult.Characters.Count == 1
            ? BuildCard(charResult.Characters[0])
            : BuildRosterCard(charResult.GroupName ?? "Adventuring Company", charResult.Characters);
    }

    internal static CardOutput BuildCard(Character character)
    {
        var className = string.IsNullOrWhiteSpace(character.ClassName) ? "No Class" : character.ClassName;
        var summary = $"{className} — HP {character.HitPoints} | Omens {character.Omens} | {character.Silver}s";

        var fields = new List<CardField>
        {
            new("Abilities", FormatAbilities(character)),
            new("Equipment", FormatEquipment(character))
        };

        var descriptionText = FormatDescriptions(character);
        if (!string.IsNullOrEmpty(descriptionText))
            fields.Add(new CardField("Description", descriptionText));

        if (!string.IsNullOrWhiteSpace(character.Vignette))
            fields.Add(new CardField("Vignette", character.Vignette));

        if (character.ScrollsKnown.Count > 0)
            fields.Add(new CardField("Scrolls", string.Join("\n", character.ScrollsKnown)));

        return new CardOutput(
            Title: character.Name,
            Description: summary,
            Footer: "MÖRK BORG is © Ockult Örtmästare Games & Stockholm Kartell. Used under the MÖRK BORG Third Party License.",
            Color: SingleColor,
            Fields: fields);
    }

    internal static CardOutput BuildRosterCard(string groupName, IReadOnlyList<Character> members)
    {
        var memberList = string.Join("\n", members.Select(m => $"• {m.Name}"));
        var description = $"{members.Count} Characters\n\n{memberList}";

        return new CardOutput(
            Title: groupName,
            Description: description,
            Footer: "MÖRK BORG is © Ockult Örtmästare Games & Stockholm Kartell. Used under the MÖRK BORG Third Party License.",
            Color: GroupColor);
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
