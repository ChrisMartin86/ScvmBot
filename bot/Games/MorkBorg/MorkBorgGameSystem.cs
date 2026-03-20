using Discord;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>MÖRK BORG implementation of <see cref="IGameSystem"/> with optional PDF support via <see cref="IGamePdfSupport"/>.</summary>
public sealed class MorkBorgGameSystem : IGameSystem, IGamePdfSupport
{
    private readonly CharacterGenerator _generator;
    private readonly MorkBorgPdfRenderer _pdfRenderer;

    public MorkBorgGameSystem(CharacterGenerator generator, MorkBorgPdfRenderer pdfRenderer)
    {
        _generator = generator;
        _pdfRenderer = pdfRenderer;
    }

    public string Name => "MÖRK BORG";

    public string CommandKey => "morkborg";

    public bool SupportsPdf => _pdfRenderer.TemplateExists;

    public SlashCommandOptionBuilder BuildCommandGroupOptions() =>
        MorkBorgCommandDefinition.BuildCommandGroupOptions();

    public async Task<GenerateResult> HandleGenerateCommandAsync(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions,
        CancellationToken ct = default)
    {
        // Determine which subcommand (character or party)
        var subcommand = subCommandOptions
            ?.FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommand);

        if (subcommand == null)
        {
            throw new InvalidOperationException(
                "No subcommand provided. Expected: /generate morkborg character|party [options]");
        }

        if (string.Equals(subcommand.Name, "party", StringComparison.OrdinalIgnoreCase))
        {
            return await HandlePartyGenerationAsync(subCommandOptions, ct);
        }

        if (string.Equals(subcommand.Name, "character", StringComparison.OrdinalIgnoreCase))
        {
            var options = MorkBorgGenerateOptionParser.Parse(subCommandOptions);
            var character = await _generator.GenerateAsync(options, ct);
            var embed = CharacterCardBuilder.Build(character, options.RollMethod);
            return new CharacterGenerationResult(character, embed);
        }

        throw new InvalidOperationException(
            $"Unknown subcommand '{subcommand.Name}'. Expected 'character' or 'party'.");
    }

    private async Task<PartyGenerationResult> HandlePartyGenerationAsync(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions,
        CancellationToken ct = default)
    {
        var partySize = MorkBorgPartyOptionParser.ParsePartySize(subCommandOptions);
        var suppliedPartyName = MorkBorgPartyOptionParser.ParsePartyName(subCommandOptions);

        var characters = new List<Character>();

        var defaultOptions = new CharacterGenerationOptions();

        for (int i = 0; i < partySize; i++)
        {
            var character = await _generator.GenerateAsync(defaultOptions, ct);
            characters.Add(character);
        }

        // Generate or use supplied party name
        var partyName = PartyNameGenerator.Generate(characters, suppliedPartyName);

        // Create party embed for Discord display
        var partyEmbed = PartyEmbedBuilder.Build(partyName, characters);

        return new PartyGenerationResult(
            Characters: characters.AsReadOnly(),
            PartyCard: partyEmbed,
            PartyName: partyName);
    }


    public byte[]? GeneratePdf(ICharacter character)
    {
        if (character is not Character mbChar)
            throw new ArgumentException($"Expected a MÖRK BORG Character but got {character.GetType().Name}.");

        return _pdfRenderer.Render(mbChar);
    }

    public string BuildFileName(ICharacter character) => BuildFileNameInternal(character);

    /// <summary>Static helper for testability without the full object graph.</summary>
    internal static string BuildFileNameInternal(ICharacter character)
    {
        var safeName = string.IsNullOrWhiteSpace(character.Name)
            ? "character"
            : new string(character.Name
                .Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_')
                .ToArray())
                .Trim('_');

        return $"{safeName}.pdf";
    }
}
