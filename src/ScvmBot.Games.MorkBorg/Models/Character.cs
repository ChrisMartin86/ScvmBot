using System.ComponentModel.DataAnnotations;

namespace ScvmBot.Games.MorkBorg.Models;

public class Character
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Presence { get; set; }
    public int Toughness { get; set; }
    public int Omens { get; set; }
    public int HitPoints { get; set; }
    public int MaxHitPoints { get; set; }
    public int Silver { get; set; }

    public string? EquippedWeapon { get; set; }

    [MaxLength(200)]
    public string? EquippedArmor { get; set; }

    public string? ClassName { get; set; }

    [MaxLength(500)]
    public string? ClassAbility { get; set; }

    public int Corruption { get; set; } = 0;

    public List<string> ScrollsKnown { get; set; } = new List<string>();

    public List<CharacterDescription> Descriptions { get; set; } = new List<CharacterDescription>();
    public List<string> Items { get; set; } = new List<string>();

    [MaxLength(1000)]
    public string? Vignette { get; set; }
}
