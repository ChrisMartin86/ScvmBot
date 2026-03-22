namespace ScvmBot.Modules;

/// <summary>Base type for results from game system generation.</summary>
public abstract record GenerateResult
{
    /// <summary>Number of characters in this result.</summary>
    public abstract int CharacterCount { get; }
}

/// <summary>Non-generic base for character generation results.</summary>
public abstract record GenerationBatch : GenerateResult;

/// <summary>Result containing one or more generated characters.</summary>
/// <typeparam name="TCharacter">The game-specific character type.</typeparam>
public sealed record GenerationBatch<TCharacter>(
    IReadOnlyList<TCharacter> Characters,
    string? GroupName = null) : GenerationBatch
{
    /// <inheritdoc />
    public override int CharacterCount => Characters.Count;
}
