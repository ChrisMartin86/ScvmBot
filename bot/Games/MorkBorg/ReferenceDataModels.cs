using System.Text.Json.Serialization;

namespace ScvmBot.Bot.Games.MorkBorg;

public class WeaponData
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

    public string ToFormattedString()
    {
        var parts = new List<string> { $"Damage: {Damage}" };

        if (IsRanged) parts.Add("Ranged");
        if (TwoHanded) parts.Add("Two-handed");
        if (!string.IsNullOrWhiteSpace(Special)) parts.Add(Special);

        return $"{Name} ({string.Join(", ", parts)})";
    }
}

public class ArmorData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public int Tier { get; set; }

    /// <summary>Damage reduction die string (e.g. "d2", "d4", "d6"). Null for no armor.</summary>
    [JsonPropertyName("damageReduction")]
    public string? DamageReduction { get; set; }

    /// <summary>Added to defense DR when wearing this armor (0 for light, 2 for medium, 4 for heavy).</summary>
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

public class ItemData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("silverValue")]
    public int? SilverValue { get; set; }

    [JsonPropertyName("isConsumable")]
    public bool IsConsumable { get; set; }

    public string ToFormattedString()
    {
        var parts = new List<string> { Category };

        if (!string.IsNullOrWhiteSpace(Description)) parts.Add(Description);
        if (SilverValue.HasValue) parts.Add($"{SilverValue}s");

        return $"{Name} ({string.Join(", ", parts)})";
    }
}

public class ScrollData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>"Unclean" or "Sacred".</summary>
    [JsonPropertyName("scrollType")]
    public string ScrollType { get; set; } = string.Empty;

    /// <summary>Position on the d10 table (1-10).</summary>
    [JsonPropertyName("scrollNumber")]
    public int ScrollNumber { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>DR to test Presence against when using this scroll.</summary>
    [JsonPropertyName("usageDR")]
    public int UsageDR { get; set; } = 12;

    public string ToFormattedString()
        => $"{Name} ({ScrollType} #{ScrollNumber}, DR{UsageDR})";
}

public class ClassData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("hitDie")]
    public string HitDie { get; set; } = "d8";

    [JsonPropertyName("omenDie")]
    public string OmenDie { get; set; } = "d2";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("classAbility")]
    public string ClassAbility { get; set; } = string.Empty;

    [JsonPropertyName("startingWeapons")]
    public List<string> StartingWeapons { get; set; } = new();

    [JsonPropertyName("startingArmor")]
    public List<string> StartingArmor { get; set; } = new();

    [JsonPropertyName("startingScrolls")]
    public List<string> StartingScrolls { get; set; } = new();

    [JsonPropertyName("startingSilver")]
    public string? StartingSilver { get; set; }

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

    // ── Starting Equipment Logic ──
    /// <summary>
    /// Controls which starting equipment flow to use.
    /// The default is "ordinary" (the standard equipment mode for classed characters).
    /// 
    /// Values and their behavior:
    /// - "classless": full classless generation flow
    ///   * waterskin + dried food (d4 days)
    ///   * starting container (d6 roll)
    ///   * classless Table A (d12)
    ///   * classless Table B (d12)
    /// - "ordinary": standard classed generation flow
    ///   * waterskin + dried food (d4 days)
    ///   * starting container (d6 roll)
    ///   * class startingItems (if any)
    ///   * NO classless random tables
    /// - "custom": class-specific only, no default base kit
    ///   * ONLY class startingItems (e.g. "Medicine chest" for Occult Herbmaster)
    ///   * NO waterskin, food, container, or random tables
    /// 
    /// Any unrecognized value will throw InvalidOperationException during generation.
    /// </summary>
    [JsonPropertyName("startingEquipmentMode")]
    public string StartingEquipmentMode { get; set; } = "ordinary";

    /// <summary>Additional starting items or features specific to this class.
    /// Supports concrete item names (e.g. "Medicine chest") and generation tokens:
    /// - random_sacred_scroll: generates a random sacred scroll
    /// - random_unclean_scroll: generates a random unclean scroll
    /// - random_any_scroll: generates a random sacred or unclean scroll
    /// </summary>
    [JsonPropertyName("startingItems")]
    public List<string> StartingItems { get; set; } = new();

    [JsonPropertyName("canUseScrolls")]
    public bool CanUseScrolls { get; set; } = true;

    [JsonPropertyName("canWearHeavyArmor")]
    public bool CanWearHeavyArmor { get; set; } = true;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    public string ToFormattedString()
        => $"{Name} (Hit Die: {HitDie}, Omens: {OmenDie}) - {ClassAbility}";
}

public class DescriptionTables
{
    [JsonPropertyName("Trait")]
    public List<string> Trait { get; set; } = new();

    [JsonPropertyName("BrokenBody")]
    public List<string> BrokenBody { get; set; } = new();

    [JsonPropertyName("BadHabit")]
    public List<string> BadHabit { get; set; } = new();

}

public class VignetteData
{
    [JsonPropertyName("Templates")]
    public List<string> Templates { get; set; } = new();

    [JsonPropertyName("ClassIntros")]
    public Dictionary<string, List<string>> ClassIntros { get; set; } = new();

    [JsonPropertyName("Bodies")]
    public Dictionary<string, List<string>> Bodies { get; set; } = new();

    [JsonPropertyName("Habits")]
    public Dictionary<string, List<string>> Habits { get; set; } = new();

    [JsonPropertyName("Items")]
    public Dictionary<string, List<string>> Items { get; set; } = new();

    [JsonPropertyName("Traits")]
    public Dictionary<string, List<string>> Traits { get; set; } = new();

    [JsonPropertyName("Closers")]
    public List<string> Closers { get; set; } = new();
}
