using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Games;
using ScvmBot.Bot.Services;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;

namespace ScvmBot.Bot.Rendering.MorkBorg;

/// <summary>
/// Renders a MÖRK BORG <see cref="PartyGenerationResult"/> as a ZIP archive
/// containing a PDF character sheet for each party member.
/// Individual member PDF failures are logged and skipped; the ZIP contains
/// whichever sheets rendered successfully. If no sheets succeed, the renderer throws.
/// </summary>
public sealed class MorkBorgPartyPdfRenderer : IResultRenderer
{
    private readonly MorkBorgPdfRenderer _pdfRenderer;
    private readonly ILogger<MorkBorgPartyPdfRenderer> _logger;

    public MorkBorgPartyPdfRenderer(MorkBorgPdfRenderer pdfRenderer, ILogger<MorkBorgPartyPdfRenderer> logger)
    {
        _pdfRenderer = pdfRenderer;
        _logger = logger;
    }

    public OutputFormat Format => OutputFormat.Pdf;

    public bool CanRender(GenerateResult result) =>
        result is PartyGenerationResult && _pdfRenderer.TemplateExists;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not PartyGenerationResult partyResult)
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a MÖRK BORG party PDF archive.");

        var memberPdfs = new List<(ICharacter Character, byte[] PdfBytes)>();
        foreach (var character in partyResult.Characters)
        {
            if (character is not Character mbChar)
            {
                _logger.LogWarning("Skipping non-MÖRK BORG character '{Name}' in party PDF.", character.Name);
                continue;
            }

            try
            {
                var pdf = _pdfRenderer.Render(mbChar);
                if (pdf is not null)
                    memberPdfs.Add((character, pdf));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF rendering failed for character '{Name}'; skipping.", character.Name);
            }
        }

        if (memberPdfs.Count == 0)
            throw new InvalidOperationException(
                "All party member PDFs failed to render.");

        var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
        var zipFileName = PartyZipBuilder.GeneratePartyZipFileName(partyResult.PartyName);
        return new FileOutput(zipBytes, zipFileName);
    }
}
