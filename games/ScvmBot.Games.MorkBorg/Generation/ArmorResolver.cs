using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Generation;

public sealed class ArmorResolver
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly DiceRoller _dice;
    private readonly Random _rng;

    public ArmorResolver(MorkBorgReferenceDataService refData, DiceRoller dice, Random rng)
    {
        _refData = refData;
        _dice = dice;
        _rng = rng;
    }

    public string? Resolve(CharacterGenerationOptions options, ClassData? classData)
    {
        if (!string.IsNullOrWhiteSpace(options.ArmorName))
        {
            var armor = _refData.GetArmorByName(options.ArmorName);
            if (armor is null)
                throw new InvalidOperationException(
                    $"Armor '{options.ArmorName}' not found in armor data.");
            return armor.ToFormattedString();
        }

        if (classData?.StartingArmor.Count > 0)
        {
            var classArmor = classData.StartingArmor[_rng.Next(classData.StartingArmor.Count)];
            var armor = _refData.GetArmorByName(classArmor);
            if (armor is null)
                throw new InvalidOperationException(
                    $"Armor '{classArmor}' listed in class '{classData.Name}' not found in armor data.");
            return armor.ToFormattedString();
        }

        // Roll for armor tier
        var armorDieSize = DiceRoller.ParseDieSize(classData?.ArmorRollDie ?? "d4");
        var roll = _dice.RollDie(armorDieSize);
        var tier = roll - 1; // d4 roll 1=>tier 0, 2=>1, 3=>2, 4=>3
        var selectedArmor = _refData.GetArmorByTier(tier);
        if (selectedArmor is null && _refData.Armor.Count > 0)
            throw new InvalidOperationException(
                $"No armor found for tier {tier}. Check armor data for a missing tier entry.");
        return selectedArmor?.ToFormattedString();
    }
}
