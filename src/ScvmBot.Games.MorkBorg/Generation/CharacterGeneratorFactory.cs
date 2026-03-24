namespace ScvmBot.Games.MorkBorg.Generation;

/// <summary>
/// Constructs a fully-wired <see cref="CharacterGenerator"/> from the two base
/// dependencies. Use this instead of calling the primary constructor directly when
/// DI is not available (e.g. tests, CLI, benchmarks).
/// </summary>
public static class CharacterGeneratorFactory
{
    public static CharacterGenerator Create(
        ScvmBot.Games.MorkBorg.Reference.MorkBorgReferenceDataService refData,
        Random? rng = null)
    {
        var resolvedRng = rng ?? Random.Shared;
        var dice = new DiceRoller(resolvedRng);
        var scrollResolver = new ScrollResolver(refData, resolvedRng);
        return new CharacterGenerator(
            refData,
            dice,
            new AbilityRoller(dice, resolvedRng),
            new WeaponResolver(refData, dice, resolvedRng),
            new ArmorResolver(refData, dice, resolvedRng),
            scrollResolver,
            new StartingGearTable(refData, dice, scrollResolver, resolvedRng),
            new VignetteGenerator(refData.Vignettes, resolvedRng),
            resolvedRng);
    }
}
