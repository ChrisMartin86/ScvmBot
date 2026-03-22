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

        if (!string.Equals(subCommand, "character", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Unknown subcommand '{subCommand}'. Expected 'character'.");

        var genOptions = MorkBorgGenerateOptionParser.Parse(options);
        var count = MorkBorgGenerateOptionParser.ParseCount(options);

        var characters = new List<Character>(count);
        for (var i = 0; i < count; i++)
        {
            // Name override only applies to the first character
            var iterOptions = i == 0 ? genOptions : new CharacterGenerationOptions
            {
                RollMethod = genOptions.RollMethod,
                ClassName = genOptions.ClassName
            };
            characters.Add(_generator.Generate(iterOptions));
        }

        var groupName = count > 1 ? PartyNameGenerator.Generate(characters) : null;

        return Task.FromResult<GenerateResult>(
            new CharacterGenerationResult<Character>(characters.AsReadOnly(), groupName));
    }
}
