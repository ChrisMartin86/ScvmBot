namespace ScvmBot.Games.CyBorg.Models;

public sealed class CyBorgCharacterGenerationOptions
{
    public string? Name { get; set; }

    // null: random 50/50 | "none": classless | empty: classless (backward compat) | other: class lookup
    public string? ClassName { get; set; }

    public int? Strength { get; set; }
    public int? Agility { get; set; }
    public int? Presence { get; set; }
    public int? Toughness { get; set; }

    public int? HitPoints { get; set; }
    public int? MaxHitPoints { get; set; }

    public int? Luck { get; set; }
    public int? Credits { get; set; }
}
