using ScvmBot.Games.CyBorg.Models;
using System.Text;

namespace ScvmBot.Modules.CyBorg;

/// <summary>
/// Renders a Cy_Borg <see cref="GenerationBatch"/> as a structured card.
/// Single character: detailed card with abilities, equipment, descriptions, and apps.
/// Multiple characters: roster card showing group name and member list.
/// </summary>
public sealed class CyBorgCharacterEmbedRenderer : IResultRenderer
{
    private static readonly CardColor SingleColor = new(0, 200, 180);
    private static readonly CardColor GroupColor = new(0, 120, 100);

    public Type ResultType => typeof(GenerationBatch<CyBorgCharacter>);

    public OutputFormat Format => OutputFormat.Card;

    public bool CanRender(GenerateResult result) =>
        result is GenerationBatch<CyBorgCharacter>;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not GenerationBatch<CyBorgCharacter> charResult)
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a Cy_Borg character card.");

        return charResult.Characters.Count == 1
            ? BuildCard(charResult.Characters[0])
            : BuildRosterCard(charResult.GroupName ?? "Crew", charResult.Characters);
    }

    internal static CardOutput BuildCard(CyBorgCharacter character)
    {
        var className = string.IsNullOrWhiteSpace(character.ClassName) ? "No Class" : character.ClassName;
        var summary = $"{className} — HP {character.HitPoints} | Luck {character.Luck} | ¢{character.Credits}";

        var fields = new List<CardField>
        {
            new("Abilities", FormatAbilities(character)),
            new("Equipment", FormatEquipment(character))
        };

        var descriptionText = FormatDescriptions(character);
        if (!string.IsNullOrEmpty(descriptionText))
            fields.Add(new CardField("Description", descriptionText));

        if (!string.IsNullOrWhiteSpace(character.ClassAbility))
            fields.Add(new CardField("Class Ability", character.ClassAbility));

        if (character.Apps.Count > 0)
            fields.Add(new CardField("Apps", string.Join("\n", character.Apps)));

        return new CardOutput(
            Title: character.Name,
            Description: summary,
            Footer: "CY_BORG is © Stockholm Kartell. Used under the CY_BORG Third Party License.",
            Color: SingleColor,
            Fields: fields);
    }

    internal static CardOutput BuildRosterCard(string groupName, IReadOnlyList<CyBorgCharacter> members)
    {
        var memberList = string.Join("\n", members.Select(m => $"• {m.Name}"));
        var description = $"{members.Count} Characters\n\n{memberList}";

        return new CardOutput(
            Title: groupName,
            Description: description,
            Footer: "CY_BORG is © Stockholm Kartell. Used under the CY_BORG Third Party License.",
            Color: GroupColor);
    }

    private static string FormatAbilities(CyBorgCharacter character) =>
        $"STR {FormatModifier(character.Strength)} · AGI {FormatModifier(character.Agility)} · PRE {FormatModifier(character.Presence)} · TGH {FormatModifier(character.Toughness)}";

    private static string FormatModifier(int value) =>
        value > 0 ? $"+{value}" : value.ToString();

    private static string FormatEquipment(CyBorgCharacter character)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.IsNullOrWhiteSpace(character.EquippedWeapon) ? "No Weapon" : character.EquippedWeapon);
        sb.AppendLine(string.IsNullOrWhiteSpace(character.EquippedArmor) ? "No Armor" : character.EquippedArmor);
        foreach (var item in character.Gear)
            sb.AppendLine(item);
        return sb.ToString().TrimEnd();
    }

    private static string FormatDescriptions(CyBorgCharacter character)
    {
        var lines = new List<string>();
        foreach (var desc in character.Descriptions)
        {
            if (desc.Category is CyBorgDescriptionCategory.Trait
                or CyBorgDescriptionCategory.Appearance
                or CyBorgDescriptionCategory.Glitch)
            {
                lines.Add($"{desc.Category}: {desc.Text}");
            }
        }
        return string.Join("\n", lines);
    }
}
