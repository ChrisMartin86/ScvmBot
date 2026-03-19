using ScvmBot.Bot.Models.MorkBorg;
using System.Text.RegularExpressions;

namespace ScvmBot.Bot.Services.MorkBorg;

/// <summary>Maps a <see cref="Character"/> to a <see cref="CharacterSheetData"/> for PDF filling.</summary>
public static class CharacterSheetMapper
{
    private static readonly Regex TierPattern = new(@"Tier\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static CharacterSheetData Map(Character character)
    {
        var data = new CharacterSheetData
        {
            Name = character.Name,
            HP_Current = character.HitPoints.ToString(),
            HP_Max = character.MaxHitPoints.ToString(),
            Strength = FormatModifier(character.Strength),
            Agility = FormatModifier(character.Agility),
            Presence = FormatModifier(character.Presence),
            Toughness = FormatModifier(character.Toughness),
            Omens = character.Omens.ToString(),
            Silver = character.Silver.ToString(),
            ClassName = FormatClassField(character.ClassName, character.ClassAbility),
            Corruption = character.Corruption.ToString(),
            Description = string.Join("\n", character.Descriptions.Where(IsTraitDescription)),
            ArmorText = character.EquippedArmor ?? string.Empty,
            ArmorTier = ParseArmorTier(character.EquippedArmor),
        };

        data.Weapons[0] = character.EquippedWeapon ?? string.Empty;
        data.Weapons[1] = string.Empty;

        for (var i = 0; i < data.Powers.Length; i++)
            data.Powers[i] = character.ScrollsKnown.Count > i ? character.ScrollsKnown[i] : string.Empty;

        var items = character.Items;
        for (var i = 0; i < data.Equipment.Length; i++)
            data.Equipment[i] = items.Count > i ? items[i] : string.Empty;

        return data;
    }

    private static string FormatModifier(int value) =>
        value >= 0 ? $"+{value}" : value.ToString();

    private static bool IsTraitDescription(string description) =>
        !description.StartsWith("Food:", StringComparison.OrdinalIgnoreCase) &&
        !description.StartsWith("Water:", StringComparison.OrdinalIgnoreCase) &&
        !description.StartsWith("Gear:", StringComparison.OrdinalIgnoreCase) &&
        !description.StartsWith("Beast:", StringComparison.OrdinalIgnoreCase);

    private static int ParseArmorTier(string? armorFormatted)
    {
        if (string.IsNullOrWhiteSpace(armorFormatted)) return 0;
        var match = TierPattern.Match(armorFormatted);
        return match.Success && int.TryParse(match.Groups[1].Value, out var tier) ? tier : 0;
    }

    private static string FormatClassField(string? className, string? classAbility)
    {
        var name = className ?? string.Empty;
        var ability = classAbility ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ability)) return name;
        if (string.IsNullOrWhiteSpace(name)) return ability;
        return $"{name}\n{ability}";
    }
}
