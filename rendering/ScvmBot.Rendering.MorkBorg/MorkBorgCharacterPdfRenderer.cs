using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;

namespace ScvmBot.Rendering.MorkBorg;

/// <summary>
/// Renders a MÖRK BORG <see cref="CharacterGenerationResult"/> as a filled PDF character sheet.
/// </summary>
public sealed class MorkBorgCharacterPdfRenderer : IResultRenderer
{
    private readonly MorkBorgPdfRenderer _pdfRenderer;

    public MorkBorgCharacterPdfRenderer(MorkBorgPdfRenderer pdfRenderer) =>
        _pdfRenderer = pdfRenderer;

    public OutputFormat Format => OutputFormat.Pdf;

    public bool CanRender(GenerateResult result) =>
        result is CharacterGenerationResult { Character: Character } && _pdfRenderer.TemplateExists;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not CharacterGenerationResult { Character: Character character })
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a MÖRK BORG character PDF.");

        var pdfBytes = _pdfRenderer.Render(character)
            ?? throw new InvalidOperationException("PDF template is not available.");

        return new FileOutput(pdfBytes, BuildFileName(character));
    }

    internal static string BuildFileName(ICharacter character)
    {
        var safeName = PartyZipBuilder.SanitizeFileName(character.Name ?? "", fallback: "character");
        return $"{safeName}.pdf";
    }
}
