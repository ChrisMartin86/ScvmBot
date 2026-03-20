using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Bot.Games.MorkBorg;

public sealed record AbilityScores(int Strength, int Agility, int Presence, int Toughness);

public sealed class AbilityRoller
{
    private readonly DiceRoller _dice;
    private readonly Random _rng;

    public AbilityRoller(DiceRoller dice, Random rng)
    {
        _dice = dice;
        _rng = rng;
    }

    public AbilityScores Roll(CharacterGenerationOptions options, ClassData? classData)
    {
        var classless = classData == null;
        var useHeroicRoll = classless && options.RollMethod == AbilityRollMethod.FourD6DropLowest;

        var heroicAbilityIndices = new HashSet<int>();
        if (useHeroicRoll)
        {
            while (heroicAbilityIndices.Count < 2)
            {
                heroicAbilityIndices.Add(_rng.Next(4));  // 0=STR, 1=AGI, 2=PRE, 3=TOU
            }
        }

        var str = options.Strength ?? RollAbilityModifier(heroicAbilityIndices.Contains(0) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);
        var agi = options.Agility ?? RollAbilityModifier(heroicAbilityIndices.Contains(1) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);
        var pre = options.Presence ?? RollAbilityModifier(heroicAbilityIndices.Contains(2) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);
        var tou = options.Toughness ?? RollAbilityModifier(heroicAbilityIndices.Contains(3) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);

        // Apply class stat modifiers only to rolled (non-overridden) abilities
        if (classData != null)
        {
            if (options.Strength == null) str = Math.Clamp(str + classData.StrengthModifier, -3, 3);
            if (options.Agility == null) agi = Math.Clamp(agi + classData.AgilityModifier, -3, 3);
            if (options.Presence == null) pre = Math.Clamp(pre + classData.PresenceModifier, -3, 3);
            if (options.Toughness == null) tou = Math.Clamp(tou + classData.ToughnessModifier, -3, 3);
        }

        return new AbilityScores(str, agi, pre, tou);
    }

    internal int RollAbilityModifier(AbilityRollMethod method)
    {
        int total = method == AbilityRollMethod.FourD6DropLowest
            ? _dice.RollFourD6DropLowest()
            : _dice.RollDie(6) + _dice.RollDie(6) + _dice.RollDie(6);

        return total switch
        {
            <= 4 => -3,
            <= 6 => -2,
            <= 8 => -1,
            <= 12 => 0,
            <= 14 => 1,
            <= 16 => 2,
            _ => 3
        };
    }
}
