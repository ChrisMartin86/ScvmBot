using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// MÖRK BORG implementation of <see cref="IGameModule"/>.
/// Owns command definition, option parsing, and generation dispatch.
/// Rendering is handled by <see cref="IResultRenderer"/> implementations
/// registered alongside this module.
/// </summary>
public sealed class MorkBorgModule : IGameModule
{
    private readonly CharacterGenerator _generator;

    public MorkBorgModule(CharacterGenerator generator, MorkBorgReferenceDataService refData)
    {
        _generator = generator;
        SubCommands = MorkBorgCommandDefinition.BuildSubCommands(
            refData.Classes.Select(c => c.Name).ToList());
    }

    public string Name => "MÖRK BORG";

    public string CommandKey => "morkborg";

    public IReadOnlyList<SubCommandDefinition> SubCommands { get; }

    public Task<GenerateResult> HandleGenerateCommandAsync(
        string subCommand,
        IReadOnlyDictionary<string, object?> options,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.Equals(subCommand, "party", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<GenerateResult>(BuildPartyResult(options));

        if (string.Equals(subCommand, "character", StringComparison.OrdinalIgnoreCase))
        {
            var genOptions = MorkBorgGenerateOptionParser.Parse(options);
            var character = _generator.Generate(genOptions);
            return Task.FromResult<GenerateResult>(new CharacterGenerationResult<Character>(character));
        }

        throw new InvalidOperationException(
            $"Unknown subcommand '{subCommand}'. Expected 'character' or 'party'.");
    }

    private PartyGenerationResult<Character> BuildPartyResult(
        IReadOnlyDictionary<string, object?> options)
    {
        var partySize = MorkBorgPartyOptionParser.ParsePartySize(options);

        var characters = Enumerable.Range(0, partySize)
            .Select(_ => _generator.Generate(new CharacterGenerationOptions()))
            .ToList();

        var partyName = PartyNameGenerator.Generate(characters);

        return new PartyGenerationResult<Character>(
            Characters: characters.AsReadOnly(),
            PartyName: partyName);
    }
}
