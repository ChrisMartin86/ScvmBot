using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Bot.Games.MorkBorg;

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
            return armor?.ToFormattedString();
        }

        if (classData?.StartingArmor.Count > 0)
        {
            var classArmor = classData.StartingArmor[_rng.Next(classData.StartingArmor.Count)];
            var armor = _refData.GetArmorByName(classArmor);
            if (armor != null)
                return armor.ToFormattedString();
        }

        // Roll for armor tier
        var armorDieSize = DiceRoller.ParseDieSize(classData?.ArmorRollDie ?? "d4");
        var roll = _dice.RollDie(armorDieSize);
        var tier = roll - 1; // d4 roll 1=>tier 0, 2=>1, 3=>2, 4=>3
        var selectedArmor = _refData.GetArmorByTier(tier);
        return selectedArmor?.ToFormattedString();
    }
}
