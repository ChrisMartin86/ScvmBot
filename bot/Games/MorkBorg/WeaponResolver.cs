using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Bot.Games.MorkBorg;

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
            return weapon?.ToFormattedString();
        }

        if (classData?.StartingWeapons.Count > 0)
        {
            var classWeapon = classData.StartingWeapons[_rng.Next(classData.StartingWeapons.Count)];
            var weapon = _refData.GetWeaponByName(classWeapon);
            if (weapon != null)
                return weapon.ToFormattedString();
        }

        // Roll on the d10 weapon table
        var weaponDieSize = DiceRoller.ParseDieSize(classData?.WeaponRollDie ?? "d10");
        var roll = _dice.RollDie(weaponDieSize);
        var weaponName = roll switch
        {
            1 => "Femur",
            2 => "Staff",
            3 => "Shortsword",
            4 => "Knife",
            5 => "Warhammer",
            6 => "Sword",
            7 => "Bow",
            8 => "Flail",
            9 => "Crossbow",
            10 => "Zweihänder",
            _ => "Knife"
        };

        var selectedWeapon = _refData.GetWeaponByName(weaponName);
        return selectedWeapon?.ToFormattedString();
    }
}
