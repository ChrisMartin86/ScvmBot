using ScvmBot.Games.CyBorg.Models;
using ScvmBot.Games.CyBorg.Reference;

namespace ScvmBot.Games.CyBorg.Generation;

public sealed class CyBorgCharacterGenerator
{
    private readonly CyBorgReferenceDataService _refData;
    private readonly CyBorgDiceRoller _dice;
    private readonly CyBorgAbilityRoller _abilityRoller;
    private readonly CyBorgRandomPicker _picker;

    public CyBorgCharacterGenerator(
        CyBorgReferenceDataService refData,
        CyBorgDiceRoller dice,
        CyBorgAbilityRoller abilityRoller,
        CyBorgRandomPicker picker)
    {
        _refData = refData;
        _dice = dice;
        _abilityRoller = abilityRoller;
        _picker = picker;
    }

    public CyBorgCharacter Generate(CyBorgCharacterGenerationOptions? options = null)
    {
        options ??= new CyBorgCharacterGenerationOptions();

        var name = options.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = _picker.PickName();
        }

        var classData = ResolveClass(options);

        var abilities = _abilityRoller.Roll(
            classData,
            options.Strength,
            options.Agility,
            options.Presence,
            options.Toughness);

        // HP = Toughness modifier + hit die, minimum 1
        var hitDieSize = CyBorgDiceRoller.ParseDieSize(classData?.HitDie ?? "d6");
        var maxHp = options.MaxHitPoints ?? Math.Max(1, abilities.Toughness + _dice.RollDie(hitDieSize));
        var hp = options.HitPoints ?? maxHp;

        var luckDieSize = CyBorgDiceRoller.ParseDieSize(classData?.LuckDie ?? "d4");
        var luck = options.Luck ?? _dice.RollDie(luckDieSize);

        // Use class-specific credits formula if defined, otherwise 2d6 × 10
        var credits = options.Credits
            ?? (classData?.StartingCredits is { } creditsFormula
                ? _dice.RollCredits(creditsFormula)
                : (_dice.RollDie(6) + _dice.RollDie(6)) * 10);

        ValidateOverrides(maxHp, hp, luck, credits);

        var weaponFormatted = ResolveWeapon(options, classData);
        var armorFormatted = ResolveArmor(options, classData);

        var gearList = new List<string>();
        ResolveStartingGear(gearList, classData);

        var appsList = new List<string>();
        ResolveStartingApps(appsList, classData);

        var descriptions = new List<CyBorgDescription>
        {
            new(CyBorgDescriptionCategory.Trait, _picker.PickTrait()),
            new(CyBorgDescriptionCategory.Appearance, _picker.PickAppearance()),
            new(CyBorgDescriptionCategory.Glitch, _picker.PickGlitch())
        };

        return new CyBorgCharacter
        {
            Name = name!,
            Strength = abilities.Strength,
            Agility = abilities.Agility,
            Presence = abilities.Presence,
            Toughness = abilities.Toughness,
            MaxHitPoints = maxHp,
            HitPoints = hp,
            Luck = luck,
            Credits = credits,
            EquippedWeapon = weaponFormatted,
            EquippedArmor = armorFormatted,
            ClassName = classData?.Name,
            ClassAbility = classData?.ClassAbility,
            Apps = appsList,
            Gear = gearList,
            Descriptions = descriptions
        };
    }

    /// <summary>
    /// ClassName states:
    /// - "none" (case-insensitive) => classless
    /// - null => omitted, rolls random 50/50
    /// - any other string => exact class name lookup
    /// - empty string => classless (backward compat)
    /// </summary>
    private CyBorgClassData? ResolveClass(CyBorgCharacterGenerationOptions options)
    {
        if (string.Equals(options.ClassName, CyBorgConstants.ClasslessClassName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (options.ClassName == null)
        {
            // 50/50 classless vs classed: d6 1-3 = classless, 4-6 = random class
            var roll = _dice.RollDie(6);
            return roll <= 3 ? null : _picker.PickClass();
        }

        // empty string => backward compat, treat as classless
        if (string.IsNullOrWhiteSpace(options.ClassName))
        {
            return null;
        }

        return _refData.GetClassByName(options.ClassName)
            ?? throw new InvalidOperationException($"Unknown class '{options.ClassName}'.");
    }

    private string? ResolveWeapon(CyBorgCharacterGenerationOptions options, CyBorgClassData? classData)
    {
        if (classData?.StartingWeapons.Count > 0)
        {
            var weaponName = classData.StartingWeapons[_dice.RollDie(classData.StartingWeapons.Count) - 1];
            var weapon = _refData.GetWeaponByName(weaponName)
                ?? throw new InvalidOperationException(
                    $"Weapon '{weaponName}' listed in class '{classData.Name}' not found in weapons data.");
            return weapon.ToFormattedString();
        }

        // Roll on the weapon table
        var weaponDieSize = CyBorgDiceRoller.ParseDieSize(classData?.WeaponRollDie ?? "d10");
        var roll = _dice.RollDie(weaponDieSize);
        var selected = _refData.GetWeaponByTableIndex(roll);
        if (selected is null && _refData.Weapons.Count > 0)
            throw new InvalidOperationException(
                $"No weapon found for table index {roll}. Check weapons data for a missing tableIndex entry.");
        return selected?.ToFormattedString();
    }

    private string? ResolveArmor(CyBorgCharacterGenerationOptions options, CyBorgClassData? classData)
    {
        if (classData?.StartingArmor.Count > 0)
        {
            var armorName = classData.StartingArmor[_dice.RollDie(classData.StartingArmor.Count) - 1];
            var armor = _refData.GetArmorByName(armorName)
                ?? throw new InvalidOperationException(
                    $"Armor '{armorName}' listed in class '{classData.Name}' not found in armor data.");
            return armor.ToFormattedString();
        }

        // Roll for armor tier: d4 roll 1=>tier 0, 2=>tier 1, 3=>tier 2, 4=>tier 3
        var armorDieSize = CyBorgDiceRoller.ParseDieSize(classData?.ArmorRollDie ?? "d4");
        var armorRoll = _dice.RollDie(armorDieSize);
        var tier = armorRoll - 1;
        var selected = _refData.GetArmorByTier(tier);
        if (selected is null && _refData.Armor.Count > 0)
            throw new InvalidOperationException(
                $"No armor found for tier {tier}. Check armor data for a missing tier entry.");
        return selected?.ToFormattedString();
    }

    private void ResolveStartingGear(List<string> gearList, CyBorgClassData? classData)
    {
        // Class-specific starting gear
        if (classData != null)
        {
            foreach (var gearName in classData.StartingGear)
            {
                gearList.Add(gearName);
            }
        }

        // Everyone starts with one random piece of gear
        var randomGear = _picker.PickGear();
        if (randomGear != null)
            gearList.Add(randomGear.ToFormattedString());
    }

    private void ResolveStartingApps(List<string> appsList, CyBorgClassData? classData)
    {
        if (classData == null) return;

        foreach (var appName in classData.StartingApps)
        {
            if (string.Equals(appName, "random_app", StringComparison.OrdinalIgnoreCase))
            {
                var app = _picker.PickApp();
                if (app != null)
                    appsList.Add(app.ToFormattedString());
            }
            else
            {
                appsList.Add(appName);
            }
        }
    }

    private static void ValidateOverrides(int maxHp, int hp, int luck, int credits)
    {
        if (maxHp < 1)
            throw new ArgumentException($"MaxHitPoints must be at least 1 (was {maxHp}).");
        if (hp < 1)
            throw new ArgumentException($"HitPoints must be at least 1 (was {hp}).");
        if (hp > maxHp)
            throw new ArgumentException($"HitPoints ({hp}) cannot exceed MaxHitPoints ({maxHp}).");
        if (luck < 0)
            throw new ArgumentException($"Luck must be non-negative (was {luck}).");
        if (credits < 0)
            throw new ArgumentException($"Credits must be non-negative (was {credits}).");
    }

    /// <summary>Backward-compatible static helper for die string parsing.</summary>
    internal static int ParseDieSize(string die) => CyBorgDiceRoller.ParseDieSize(die);
}
