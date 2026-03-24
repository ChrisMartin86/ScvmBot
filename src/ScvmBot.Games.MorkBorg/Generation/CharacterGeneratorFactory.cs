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
        var picker = new ScvmBot.Games.MorkBorg.Reference.MorkBorgRandomPicker(refData, resolvedRng);
        var scrollResolver = new ScrollResolver(refData, picker);
        return new CharacterGenerator(
            refData,
            dice,
            new AbilityRoller(dice, resolvedRng),
            new WeaponResolver(refData, dice, resolvedRng),
            new ArmorResolver(refData, dice, resolvedRng),
            scrollResolver,
            new StartingGearTable(refData, dice, scrollResolver, picker),
            new VignetteGenerator(refData.Vignettes, resolvedRng),
            picker);
    }
}
