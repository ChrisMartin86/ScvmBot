namespace ScvmBot.Games.CyBorg.Generation;

/// <summary>
/// Constructs a fully-wired <see cref="CyBorgCharacterGenerator"/> from the two base
/// dependencies. Use this instead of calling the primary constructor directly when
/// DI is not available (e.g. tests, CLI, benchmarks).
/// </summary>
public static class CyBorgCharacterGeneratorFactory
{
    public static CyBorgCharacterGenerator Create(
        ScvmBot.Games.CyBorg.Reference.CyBorgReferenceDataService refData,
        Random? rng = null)
    {
        var resolvedRng = rng ?? Random.Shared;
        var dice = new CyBorgDiceRoller(resolvedRng);
        var picker = new ScvmBot.Games.CyBorg.Reference.CyBorgRandomPicker(refData, resolvedRng);
        return new CyBorgCharacterGenerator(
            refData,
            dice,
            new CyBorgAbilityRoller(dice),
            picker);
    }
}
