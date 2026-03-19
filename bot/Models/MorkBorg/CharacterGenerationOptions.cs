namespace ScvmBot.Bot.Models.MorkBorg;

/// <summary>Determines how ability score modifiers are rolled during character generation.</summary>
public enum AbilityRollMethod
{
    /// <summary>Roll 3d6, sum, apply the MÖRK BORG modifier table.</summary>
    ThreeD6 = 1,

    /// <summary>Roll 4d6, drop the lowest die, sum, apply the modifier table. Produces slightly higher stats on average.</summary>
    FourD6DropLowest = 2
}

public sealed class CharacterGenerationOptions
{
    public AbilityRollMethod RollMethod { get; set; } = AbilityRollMethod.ThreeD6;

    public string? Name { get; set; }
    public string? Description { get; set; }

    public int? Strength { get; set; }
    public int? Agility { get; set; }
    public int? Presence { get; set; }
    public int? Toughness { get; set; }

    public int? Omens { get; set; }

    public int? HitPoints { get; set; }
    public int? MaxHitPoints { get; set; }

    public int? Silver { get; set; }

    public string? WeaponName { get; set; }
    public string? ArmorName { get; set; }

    // null: random 50/50 | "none": classless | empty: classless (backward compat) | other: class lookup
    public string? ClassName { get; set; }

    public string? StartingContainerOverride { get; set; }

    public bool SkipRandomStartingGear { get; set; } = false;
    public int? StartingGearRollA { get; set; }
    public int? StartingGearRollB { get; set; }

    public List<string> ForceItemNames { get; } = new();
}
