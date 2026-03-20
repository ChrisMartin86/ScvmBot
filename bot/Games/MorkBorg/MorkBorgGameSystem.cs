using Discord;
using ScvmBot.Bot.Services;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>MÖRK BORG implementation of <see cref="IGameSystem"/> with optional PDF support via <see cref="IGamePdfSupport"/>.</summary>
public sealed class MorkBorgGameSystem : IGameSystem, IGamePdfSupport
{
    private readonly CharacterGenerator _generator;
    private readonly MorkBorgPdfRenderer _pdfRenderer;
    private readonly MorkBorgReferenceDataService _refData;

    public MorkBorgGameSystem(CharacterGenerator generator, MorkBorgPdfRenderer pdfRenderer, MorkBorgReferenceDataService refData)
    {
        _generator = generator;
        _pdfRenderer = pdfRenderer;
        _refData = refData;
    }

    public string Name => "MÖRK BORG";

    public string CommandKey => "morkborg";

    public bool SupportsPdf => _pdfRenderer.TemplateExists;

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
            var embed = CharacterCardBuilder.Build(character, options.RollMethod);
            return Task.FromResult<GenerateResult>(new CharacterGenerationResult(character, embed));
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
        var safeName = PartyZipBuilder.SanitizeFileName(character.Name ?? "", fallback: "character");
        return $"{safeName}.pdf";
    }
}
