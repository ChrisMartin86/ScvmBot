namespace ScvmBot.Modules;

/// <summary>Base type for results from game system generation.</summary>
public abstract record GenerateResult;

/// <summary>Result for a single character generation.</summary>
public sealed record CharacterGenerationResult(
    object Character) : GenerateResult;

/// <summary>Result for party generation with multiple characters.</summary>
public sealed record PartyGenerationResult(
    IReadOnlyList<object> Characters,
    string PartyName) : GenerateResult;
