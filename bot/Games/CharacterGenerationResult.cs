using Discord;
using ScvmBot.Bot.Models;

namespace ScvmBot.Bot.Games;

/// <summary>Base type for results from <see cref="IGameSystem.HandleGenerateCommandAsync"/>.</summary>
public abstract record GenerateResult;

/// <summary>Result for a single character generation.</summary>
public sealed record CharacterGenerationResult(
    ICharacter Character,
    Embed Card) : GenerateResult;

/// <summary>Result for party generation with multiple characters.</summary>
public sealed record PartyGenerationResult(
    IReadOnlyList<ICharacter> Characters,
    Embed PartyCard,
    string PartyName) : GenerateResult;
