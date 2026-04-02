using System.Text.Json.Serialization;

namespace ScvmBot.Games.CyBorg.Reference;

public class CyBorgWeaponData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("damage")]
    public string Damage { get; set; } = string.Empty;

    [JsonPropertyName("isRanged")]
    public bool IsRanged { get; set; }

    [JsonPropertyName("twoHanded")]
    public bool TwoHanded { get; set; }

    [JsonPropertyName("special")]
    public string? Special { get; set; }

    /// <summary>
    /// 1-based position in the random weapon roll table. Only set for weapons that
    /// appear on the equipment-table roll; weapons without a table slot leave this null.
    /// </summary>
    [JsonPropertyName("tableIndex")]
    public int? TableIndex { get; set; }

    public string ToFormattedString()
    {
        var parts = new List<string> { $"Damage: {Damage}" };

        if (IsRanged) parts.Add("Ranged");
        if (TwoHanded) parts.Add("Two-handed");
        if (!string.IsNullOrWhiteSpace(Special)) parts.Add(Special);

        return $"{Name} ({string.Join(", ", parts)})";
    }
}

public class CyBorgArmorData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public int Tier { get; set; }

    /// <summary>Damage reduction die string (e.g. "d2", "d4", "d6"). Null for no armor.</summary>
    [JsonPropertyName("damageReduction")]
    public string? DamageReduction { get; set; }

    [JsonPropertyName("agilityPenalty")]
    public int AgilityPenalty { get; set; }

    public string ToFormattedString()
    {
        var parts = new List<string> { $"Tier {Tier}" };

        if (!string.IsNullOrEmpty(DamageReduction)) parts.Add($"DR: -{DamageReduction}");
        if (AgilityPenalty > 0) parts.Add($"Defense +{AgilityPenalty} DR");

        return $"{Name} ({string.Join(", ", parts)})";
    }
}

public class CyBorgGearData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    public string ToFormattedString()
    {
        if (!string.IsNullOrWhiteSpace(Description))
            return $"{Name} ({Description})";
        return Name;
    }
}

public class CyBorgAppData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("appNumber")]
    public int AppNumber { get; set; }

    public string ToFormattedString()
        => $"{Name} (App #{AppNumber})";
}

/// <summary>Structured credits-roll definition: roll DiceCount d DiceSides × Multiplier.</summary>
public sealed record CyBorgCreditsFormula(
    [property: JsonPropertyName("diceCount")] int DiceCount,
    [property: JsonPropertyName("diceSides")] int DiceSides,
    [property: JsonPropertyName("multiplier")] int Multiplier);

public class CyBorgClassData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("hitDie")]
    public string HitDie { get; set; } = "d6";

    [JsonPropertyName("luckDie")]
    public string LuckDie { get; set; } = "d4";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("classAbility")]
    public string ClassAbility { get; set; } = string.Empty;

    [JsonPropertyName("startingWeapons")]
    public IReadOnlyList<string> StartingWeapons { get; set; } = [];

    [JsonPropertyName("startingArmor")]
    public IReadOnlyList<string> StartingArmor { get; set; } = [];

    [JsonPropertyName("startingApps")]
    public IReadOnlyList<string> StartingApps { get; set; } = [];

    [JsonPropertyName("startingCredits")]
    public CyBorgCreditsFormula? StartingCredits { get; set; }

    [JsonPropertyName("strengthModifier")]
    public int StrengthModifier { get; set; } = 0;

    [JsonPropertyName("agilityModifier")]
    public int AgilityModifier { get; set; } = 0;

    [JsonPropertyName("presenceModifier")]
    public int PresenceModifier { get; set; } = 0;

    [JsonPropertyName("toughnessModifier")]
    public int ToughnessModifier { get; set; } = 0;

    [JsonPropertyName("weaponRollDie")]
    public string? WeaponRollDie { get; set; }

    [JsonPropertyName("armorRollDie")]
    public string? ArmorRollDie { get; set; }

    [JsonPropertyName("startingGear")]
    public IReadOnlyList<string> StartingGear { get; set; } = [];

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    public string ToFormattedString()
        => $"{Name} (Hit Die: {HitDie}, Luck: {LuckDie}) - {ClassAbility}";
}

public class CyBorgDescriptionTables
{
    [JsonPropertyName("Trait")]
    public IReadOnlyList<string> Trait { get; set; } = [];

    [JsonPropertyName("Appearance")]
    public IReadOnlyList<string> Appearance { get; set; } = [];

    [JsonPropertyName("Glitch")]
    public IReadOnlyList<string> Glitch { get; set; } = [];
}
