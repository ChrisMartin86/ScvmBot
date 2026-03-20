namespace ScvmBot.Bot.Games.MorkBorg;

public sealed class DiceRoller
{
    private readonly Random _rng;

    public DiceRoller(Random rng)
    {
        _rng = rng;
    }

    public int RollDie(int sides)
    {
        if (sides <= 0) throw new ArgumentOutOfRangeException(nameof(sides));
        return _rng.Next(1, sides + 1);
    }

    public int RollFourD6DropLowest()
    {
        var rolls = new[] { RollDie(6), RollDie(6), RollDie(6), RollDie(6) };
        return rolls.Sum() - rolls.Min();
    }

    /// <summary>Parses a die string like "d8" or "d10" and returns the numeric size.</summary>
    public static int ParseDieSize(string die)
    {
        if (string.IsNullOrWhiteSpace(die)) return 8;
        var numeric = die.TrimStart('d', 'D');
        return int.TryParse(numeric, out var size) && size > 0 ? size : 8;
    }

    public int RollSilver(string formula)
    {
        return formula.ToLowerInvariant() switch
        {
            "d6x10" => RollDie(6) * 10,
            "2d6x10" => (RollDie(6) + RollDie(6)) * 10,
            "d6x10x3" => RollDie(6) * 10 * 3,
            _ => throw new InvalidOperationException($"Unsupported silver formula '{formula}'.")
        };
    }
}
