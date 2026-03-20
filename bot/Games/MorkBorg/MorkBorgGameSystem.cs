using Discord;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>
/// MÖRK BORG implementation of <see cref="IGameSystem"/>.
/// Responsible only for generation; rendering is handled by
/// <see cref="Rendering.IResultRenderer"/> implementations.
/// </summary>
public sealed class MorkBorgGameSystem : IGameSystem
{
    private readonly CharacterGenerator _generator;
    private readonly MorkBorgReferenceDataService _refData;

    public MorkBorgGameSystem(CharacterGenerator generator, MorkBorgReferenceDataService refData)
    {
        _generator = generator;
        _refData = refData;
    }

    public string Name => "MÖRK BORG";

    public string CommandKey => "morkborg";

    public SlashCommandOptionBuilder BuildCommandGroupOptions() =>
        MorkBorgCommandDefinition.BuildCommandGroupOptions(
            _refData.Classes.Select(c => c.Name).ToList());

    public Task<GenerateResult> HandleGenerateCommandAsync(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions,
        CancellationToken ct = default)
    {
        var subcommand = subCommandOptions
            ?.FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommand);

        if (subcommand == null)
            throw new InvalidOperationException(
                "No subcommand provided. Expected: /generate morkborg character|party [options]");

        if (string.Equals(subcommand.Name, "party", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<GenerateResult>(BuildPartyResult(subCommandOptions));

        if (string.Equals(subcommand.Name, "character", StringComparison.OrdinalIgnoreCase))
        {
            var options = MorkBorgGenerateOptionParser.Parse(subCommandOptions);
            var character = _generator.Generate(options);
            return Task.FromResult<GenerateResult>(new CharacterGenerationResult(character));
        }

        throw new InvalidOperationException(
            $"Unknown subcommand '{subcommand.Name}'. Expected 'character' or 'party'.");
    }

    private PartyGenerationResult BuildPartyResult(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions)
    {
        var partySize = MorkBorgPartyOptionParser.ParsePartySize(subCommandOptions);

        var characters = Enumerable.Range(0, partySize)
            .Select(_ => _generator.Generate(new CharacterGenerationOptions()))
            .ToList();

        var partyName = PartyNameGenerator.Generate(characters);

        return new PartyGenerationResult(
            Characters: characters.AsReadOnly(),
            PartyName: partyName);
    }
}
