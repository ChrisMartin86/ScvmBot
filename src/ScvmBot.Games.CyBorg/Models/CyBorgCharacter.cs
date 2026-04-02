using System.ComponentModel.DataAnnotations;

namespace ScvmBot.Games.CyBorg.Models;

public class CyBorgCharacter
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Presence { get; set; }
    public int Toughness { get; set; }

    public int HitPoints { get; set; }
    public int MaxHitPoints { get; set; }

    public int Luck { get; set; }
    public int Credits { get; set; }

    public string? EquippedWeapon { get; set; }

    [MaxLength(200)]
    public string? EquippedArmor { get; set; }

    public string? ClassName { get; set; }

    [MaxLength(500)]
    public string? ClassAbility { get; set; }

    /// <summary>Nanograms or other class-specific programs (equivalent of scrolls).</summary>
    public List<string> Apps { get; set; } = new List<string>();

    public List<string> Gear { get; set; } = new List<string>();

    public List<CyBorgDescription> Descriptions { get; set; } = new List<CyBorgDescription>();
}
