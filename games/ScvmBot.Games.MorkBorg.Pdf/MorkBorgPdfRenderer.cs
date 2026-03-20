using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Pdf;

/// <summary>
/// Loads the MÖRK BORG character sheet PDF template and fills it for a given character.
/// Owns the canonical template path so no other project needs to know where the file lives.
/// </summary>
public sealed class MorkBorgPdfRenderer
{
    /// <summary>Runtime path where the PDF template is expected after build/publish.</summary>
    public static readonly string DefaultTemplatePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "character_sheet.pdf");

    private readonly byte[]? _templateBytes;

    public MorkBorgPdfRenderer() : this(DefaultTemplatePath) { }

    public MorkBorgPdfRenderer(string templatePath)
    {
        _templateBytes = File.Exists(templatePath)
            ? File.ReadAllBytes(templatePath)
            : null;
    }

    /// <summary>Returns true when the PDF template was loaded successfully at construction.</summary>
    public bool TemplateExists => _templateBytes is not null;

    /// <summary>
    /// Fills the character sheet template with <paramref name="character"/> data and returns
    /// the filled PDF bytes, or null if the template was not available at startup.
    /// </summary>
    public byte[]? Render(Character character)
    {
        if (_templateBytes is null)
            return null;

        return _templateBytes.FillMorkBorgSheet(character, flatten: true);
    }
}
