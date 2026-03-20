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

    private readonly string _templatePath;

    public MorkBorgPdfRenderer() : this(DefaultTemplatePath) { }

    public MorkBorgPdfRenderer(string templatePath)
    {
        _templatePath = templatePath;
    }

    /// <summary>Returns true when the PDF template file is present on disk.</summary>
    public bool TemplateExists => File.Exists(_templatePath);

    /// <summary>
    /// Fills the character sheet template with <paramref name="character"/> data and returns
    /// the filled PDF bytes, or null if the template file is not present.
    /// </summary>
    public byte[]? Render(Character character)
    {
        if (!File.Exists(_templatePath))
            return null;

        var templateBytes = File.ReadAllBytes(_templatePath);
        return templateBytes.FillMorkBorgSheet(character, flatten: true);
    }
}
