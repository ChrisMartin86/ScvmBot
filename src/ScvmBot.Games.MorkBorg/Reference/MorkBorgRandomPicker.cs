using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Reference;

/// <summary>
/// Handles all random-selection operations against loaded reference data.
/// Keeps random-selection logic out of <see cref="MorkBorgReferenceDataService"/>,
/// which is responsible only for data loading and indexed lookup.
/// </summary>
public sealed class MorkBorgRandomPicker
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly Random _rng;

    public MorkBorgRandomPicker(MorkBorgReferenceDataService refData, Random rng)
    {
        _refData = refData;
        _rng = rng;
    }

    public string PickName() =>
        _refData.Names.Count > 0 ? _refData.Names[_rng.Next(_refData.Names.Count)] : "Unknown";

    public WeaponData? PickWeapon() =>
        _refData.Weapons.Count > 0 ? _refData.Weapons[_rng.Next(_refData.Weapons.Count)] : null;

    public ArmorData? PickArmor() =>
        _refData.Armor.Count > 0 ? _refData.Armor[_rng.Next(_refData.Armor.Count)] : null;

    public ItemData? PickItem() =>
        _refData.Items.Count > 0 ? _refData.Items[_rng.Next(_refData.Items.Count)] : null;

    public ClassData? PickClass() =>
        _refData.Classes.Count > 0 ? _refData.Classes[_rng.Next(_refData.Classes.Count)] : null;

    public string PickTrait() =>
        _refData.Descriptions.Trait.Count > 0
            ? _refData.Descriptions.Trait[_rng.Next(_refData.Descriptions.Trait.Count)]
            : "";

    public string PickBody() =>
        _refData.Descriptions.BrokenBody.Count > 0
            ? _refData.Descriptions.BrokenBody[_rng.Next(_refData.Descriptions.BrokenBody.Count)]
            : "";

    public string PickHabit() =>
        _refData.Descriptions.BadHabit.Count > 0
            ? _refData.Descriptions.BadHabit[_rng.Next(_refData.Descriptions.BadHabit.Count)]
            : "";

    public ScrollData? PickScroll(ScrollKind kind)
    {
        var matching = _refData.Scrolls.Where(s => s.Kind == kind).ToList();
        return matching.Count > 0 ? matching[_rng.Next(matching.Count)] : null;
    }

    /// <summary>
    /// Picks uniformly from the merged pool of Sacred and Unclean scrolls.
    /// Throws if no scrolls of either kind exist.
    /// </summary>
    public string PickAnyScroll()
    {
        var all = _refData.Scrolls
            .Where(s => s.Kind == ScrollKind.Sacred || s.Kind == ScrollKind.Unclean)
            .ToList();

        if (all.Count == 0)
            throw new InvalidOperationException("No sacred or unclean scrolls are available.");

        return all[_rng.Next(all.Count)].ToFormattedString();
    }
}
