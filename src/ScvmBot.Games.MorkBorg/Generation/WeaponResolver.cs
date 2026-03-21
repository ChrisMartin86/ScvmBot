using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Generation;

public sealed class WeaponResolver
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly DiceRoller _dice;
    private readonly Random _rng;

    public WeaponResolver(MorkBorgReferenceDataService refData, DiceRoller dice, Random rng)
    {
        _refData = refData;
        _dice = dice;
        _rng = rng;
    }

    public string? Resolve(CharacterGenerationOptions options, ClassData? classData)
    {
        if (!string.IsNullOrWhiteSpace(options.WeaponName))
        {
            var weapon = _refData.GetWeaponByName(options.WeaponName);
            if (weapon is null)
                throw new InvalidOperationException(
                    $"Weapon '{options.WeaponName}' not found in weapons data.");
            return weapon.ToFormattedString();
        }

        if (classData?.StartingWeapons.Count > 0)
        {
            var classWeapon = classData.StartingWeapons[_rng.Next(classData.StartingWeapons.Count)];
            var weapon = _refData.GetWeaponByName(classWeapon);
            if (weapon is null)
                throw new InvalidOperationException(
                    $"Weapon '{classWeapon}' listed in class '{classData.Name}' not found in weapons data.");
            return weapon.ToFormattedString();
        }

        // Roll on the weapon table — result maps to the weapon whose tableIndex matches
        var weaponDieSize = DiceRoller.ParseDieSize(classData?.WeaponRollDie ?? "d10");
        var roll = _dice.RollDie(weaponDieSize);
        var selectedWeapon = _refData.GetWeaponByTableIndex(roll);
        if (selectedWeapon is null && _refData.Weapons.Count > 0)
            throw new InvalidOperationException(
                $"No weapon found for table index {roll} (die: {classData?.WeaponRollDie ?? "d10"}). Check weapons data for a missing tableIndex entry.");
        return selectedWeapon?.ToFormattedString();
    }
}
