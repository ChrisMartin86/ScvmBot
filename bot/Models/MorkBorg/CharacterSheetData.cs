using System.Diagnostics.CodeAnalysis;

namespace ScvmBot.Bot.Models.MorkBorg;

/// <summary>Strongly-typed representation of every fillable field on the MÖRK BORG character sheet PDF.</summary>
[ExcludeFromCodeCoverage(Justification = "Pure data model; no logic to test.")]
public sealed class CharacterSheetData
{

    public string Name { get; set; } = string.Empty;
    public string HP_Current { get; set; } = string.Empty;
    public string HP_Max { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Agility { get; set; } = string.Empty;
    public string Presence { get; set; } = string.Empty;
    public string Toughness { get; set; } = string.Empty;
    public string Omens { get; set; } = string.Empty;
    public string Silver { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Corruption { get; set; } = string.Empty;


    public string Description { get; set; } = string.Empty;

    /// <summary>Filled into the "armor_name" text field (separate from the tier checkboxes).</summary>
    public string ArmorText { get; set; } = string.Empty;

    /// <summary>Maps to weapon_1 and weapon_2.</summary>
    public string[] Weapons { get; set; } = new string[2];

    /// <summary>Maps to powers_1 through powers_4.</summary>
    public string[] Powers { get; set; } = new string[4];

    /// <summary>Maps to equipment_1 through equipment_15.</summary>
    public string[] Equipment { get; set; } = new string[15];

    /// <summary>
    /// Armor tier 0–3. Checks only the matching armor die checkbox (armor_d2/d4/d6).
    /// </summary>
    public int ArmorTier { get; set; }

    /// <summary>Set of Miseries indices (0–5) that should be checked.</summary>
    public HashSet<int> Miseries { get; set; } = new();
}
