namespace ScvmBot.Games.CyBorg.Reference;

/// <summary>
/// Handles all random-selection operations against loaded reference data.
/// </summary>
public sealed class CyBorgRandomPicker
{
    private readonly CyBorgReferenceDataService _refData;
    private readonly Random _rng;

    public CyBorgRandomPicker(CyBorgReferenceDataService refData, Random rng)
    {
        _refData = refData;
        _rng = rng;
    }

    public string PickName() =>
        _refData.Names.Count > 0 ? _refData.Names[_rng.Next(_refData.Names.Count)] : "Unknown";

    public CyBorgWeaponData? PickWeapon() =>
        _refData.Weapons.Count > 0 ? _refData.Weapons[_rng.Next(_refData.Weapons.Count)] : null;

    public CyBorgArmorData? PickArmor() =>
        _refData.Armor.Count > 0 ? _refData.Armor[_rng.Next(_refData.Armor.Count)] : null;

    public CyBorgGearData? PickGear() =>
        _refData.Gear.Count > 0 ? _refData.Gear[_rng.Next(_refData.Gear.Count)] : null;

    public CyBorgClassData? PickClass() =>
        _refData.Classes.Count > 0 ? _refData.Classes[_rng.Next(_refData.Classes.Count)] : null;

    public string PickTrait() =>
        _refData.Descriptions.Trait.Count > 0
            ? _refData.Descriptions.Trait[_rng.Next(_refData.Descriptions.Trait.Count)]
            : "";

    public string PickAppearance() =>
        _refData.Descriptions.Appearance.Count > 0
            ? _refData.Descriptions.Appearance[_rng.Next(_refData.Descriptions.Appearance.Count)]
            : "";

    public string PickGlitch() =>
        _refData.Descriptions.Glitch.Count > 0
            ? _refData.Descriptions.Glitch[_rng.Next(_refData.Descriptions.Glitch.Count)]
            : "";

    public CyBorgAppData? PickApp() =>
        _refData.Apps.Count > 0 ? _refData.Apps[_rng.Next(_refData.Apps.Count)] : null;
}
