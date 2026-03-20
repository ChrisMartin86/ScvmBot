using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Bot.Games;

/// <summary>
/// Optional capability interface for game systems that support PDF output.
/// Game systems that produce PDF character sheets implement this interface;
/// those that do not simply omit it. The bot checks for this capability at
/// runtime via a cast rather than requiring every game system to carry PDF
/// concerns.
/// </summary>
public interface IGamePdfSupport
{
    /// <summary>Returns true when a PDF template is available and generation is supported.</summary>
    bool SupportsPdf { get; }

    /// <summary>Generates a PDF for the given character. Returns null if generation is not possible.</summary>
    byte[]? GeneratePdf(ICharacter character);

    /// <summary>Returns the suggested file name for the generated PDF.</summary>
    string BuildFileName(ICharacter character);
}
