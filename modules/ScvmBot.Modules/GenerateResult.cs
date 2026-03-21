namespace ScvmBot.Modules;

/// <summary>Base type for results from game system generation.</summary>
public abstract record GenerateResult;

/// <summary>Non-generic base for single character generation results.</summary>
public abstract record CharacterGenerationResult : GenerateResult;

/// <summary>Result for a single character generation.</summary>
/// <typeparam name="TCharacter">The game-specific character type.</typeparam>
public sealed record CharacterGenerationResult<TCharacter>(
    TCharacter Character) : CharacterGenerationResult;

/// <summary>Non-generic base for party generation results.</summary>
public abstract record PartyGenerationResult(string PartyName) : GenerateResult
{
    /// <summary>Number of characters in the party.</summary>
    public abstract int CharacterCount { get; }
}

/// <summary>Result for party generation with multiple characters.</summary>
/// <typeparam name="TCharacter">The game-specific character type.</typeparam>
public sealed record PartyGenerationResult<TCharacter>(
    IReadOnlyList<TCharacter> Characters,
    string PartyName) : PartyGenerationResult(PartyName)
{
    /// <inheritdoc />
    public override int CharacterCount => Characters.Count;
}
