using Microsoft.Extensions.Logging;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Renders a MÖRK BORG <see cref="CharacterGenerationResult"/> as a file.
/// Single character: PDF character sheet.
/// Multiple characters: ZIP archive containing a PDF for each character.
/// Individual member PDF failures are logged and skipped; the ZIP contains
/// whichever sheets rendered successfully. If no sheets succeed, the renderer throws.
/// </summary>
public sealed class MorkBorgCharacterPdfRenderer : IResultRenderer
{
    private readonly MorkBorgPdfRenderer _pdfRenderer;
    private readonly ILogger<MorkBorgCharacterPdfRenderer> _logger;

    public MorkBorgCharacterPdfRenderer(MorkBorgPdfRenderer pdfRenderer, ILogger<MorkBorgCharacterPdfRenderer> logger)
    {
        _pdfRenderer = pdfRenderer;
        _logger = logger;
    }

    public Type ResultType => typeof(CharacterGenerationResult<Character>);

    public OutputFormat Format => OutputFormat.File;

    public bool CanRender(GenerateResult result) =>
        result is CharacterGenerationResult<Character> && _pdfRenderer.TemplateExists;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not CharacterGenerationResult<Character> charResult)
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a MÖRK BORG character PDF.");

        return charResult.Characters.Count == 1
            ? RenderSingle(charResult.Characters[0])
            : RenderZip(charResult);
    }

    private FileOutput RenderSingle(Character character)
    {
        var pdfBytes = _pdfRenderer.Render(character)
            ?? throw new InvalidOperationException("PDF template is not available.");
        return new FileOutput(pdfBytes, BuildFileName(character));
    }

    private FileOutput RenderZip(CharacterGenerationResult<Character> charResult)
    {
        var memberPdfs = new List<(string CharacterName, byte[] PdfBytes)>();
        foreach (var character in charResult.Characters)
        {
            try
            {
                var pdf = _pdfRenderer.Render(character);
                if (pdf is not null)
                    memberPdfs.Add((character.Name, pdf));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF rendering failed for character '{Name}'; skipping.", character.Name);
            }
        }

        if (memberPdfs.Count == 0)
            throw new InvalidOperationException("All character PDFs failed to render.");

        var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
        var zipFileName = PartyZipBuilder.GeneratePartyZipFileName(charResult.GroupName ?? "characters");
        return new FileOutput(zipBytes, zipFileName);
    }

    internal static string BuildFileName(Character character)
    {
        var safeName = PartyZipBuilder.SanitizeFileName(character.Name ?? "", fallback: "character");
        return $"{safeName}.pdf";
    }
}
