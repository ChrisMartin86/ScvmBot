using ScvmBot.Games.CyBorg.Reference;

namespace ScvmBot.Games.CyBorg.Generation;

public sealed record CyBorgAbilityScores(int Strength, int Agility, int Presence, int Toughness);

public sealed class CyBorgAbilityRoller
{
    private readonly CyBorgDiceRoller _dice;

    public CyBorgAbilityRoller(CyBorgDiceRoller dice)
    {
        _dice = dice;
    }

    public CyBorgAbilityScores Roll(CyBorgClassData? classData, int? strOverride, int? agiOverride, int? preOverride, int? tghOverride)
    {
        ValidateStatOverride(strOverride, nameof(strOverride));
        ValidateStatOverride(agiOverride, nameof(agiOverride));
        ValidateStatOverride(preOverride, nameof(preOverride));
        ValidateStatOverride(tghOverride, nameof(tghOverride));

        var str = strOverride ?? RollAbilityModifier();
        var agi = agiOverride ?? RollAbilityModifier();
        var pre = preOverride ?? RollAbilityModifier();
        var tgh = tghOverride ?? RollAbilityModifier();

        // Apply class stat modifiers only to rolled (non-overridden) abilities
        if (classData != null)
        {
            if (strOverride == null) str = Math.Clamp(str + classData.StrengthModifier, -3, 3);
            if (agiOverride == null) agi = Math.Clamp(agi + classData.AgilityModifier, -3, 3);
            if (preOverride == null) pre = Math.Clamp(pre + classData.PresenceModifier, -3, 3);
            if (tghOverride == null) tgh = Math.Clamp(tgh + classData.ToughnessModifier, -3, 3);
        }

        return new CyBorgAbilityScores(str, agi, pre, tgh);
    }

    /// <summary>Rolls 3d6 and maps the total to the standard ability modifier (-3 to +3).</summary>
    internal int RollAbilityModifier()
    {
        int total = _dice.RollDie(6) + _dice.RollDie(6) + _dice.RollDie(6);

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

    private static void ValidateStatOverride(int? value, string name)
    {
        if (value is not null && (value < -3 || value > 3))
            throw new ArgumentException($"{name} override must be between -3 and 3 (was {value}).");
    }
}
