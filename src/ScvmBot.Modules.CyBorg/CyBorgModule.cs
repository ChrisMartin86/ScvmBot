using ScvmBot.Games.CyBorg.Generation;
using ScvmBot.Games.CyBorg.Models;
using ScvmBot.Games.CyBorg.Reference;

namespace ScvmBot.Modules.CyBorg;

/// <summary>
/// Cy_Borg implementation of <see cref="IGameModule"/>.
/// Owns command definition, option parsing, and generation dispatch.
/// Rendering is handled by <see cref="IResultRenderer"/> implementations
/// registered alongside this module.
/// </summary>
public sealed class CyBorgModule : IGameModule
{
    private readonly CyBorgCharacterGenerator _generator;

    public CyBorgModule(CyBorgCharacterGenerator generator, CyBorgReferenceDataService refData)
    {
        _generator = generator;
        SubCommands = CyBorgCommandDefinition.BuildSubCommands(
            refData.Classes.Select(c => c.Name).ToList());
    }

    public string Name => "Cy_Borg";

    public string CommandKey => "cyborg";

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

        var genOptions = CyBorgGenerateOptionParser.Parse(options);
        var count = CyBorgGenerateOptionParser.ParseCount(options);

        var characters = new List<CyBorgCharacter>(count);
        for (var i = 0; i < count; i++)
        {
            // Name override applies only to the first character.
            var iterOptions = i == 0 ? genOptions : new CyBorgCharacterGenerationOptions
            {
                ClassName = genOptions.ClassName
            };
            characters.Add(_generator.Generate(iterOptions));
        }

        var groupName = count > 1 ? CyBorgGroupNameGenerator.Generate(characters.AsReadOnly()) : null;

        return Task.FromResult<GenerateResult>(
            new GenerationBatch<CyBorgCharacter>(characters.AsReadOnly(), groupName));
    }
}
