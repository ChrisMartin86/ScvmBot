namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Parses command options for party generation.
/// Extracts the optional "size" parameter from the options dictionary.
/// Party names are always generated randomly; there is no user-supplied name option.
/// </summary>
public sealed class MorkBorgPartyOptionParser
{
    private const int DefaultPartySize = 4;
    private const int MinPartySize = 1;
    private const int MaxPartySize = 4;

    /// <summary>
    /// Parses party options to extract party size.
    /// Returns <see cref="DefaultPartySize"/> if size is not specified or invalid.
    /// Clamps the size to <see cref="MinPartySize"/> - <see cref="MaxPartySize"/>.
    /// </summary>
    public static int ParsePartySize(IReadOnlyDictionary<string, object?> options)
    {
        if (!options.TryGetValue("size", out var sizeValue) || sizeValue is null)
            return DefaultPartySize;

        if (sizeValue is long longValue)
            return Math.Clamp((int)longValue, MinPartySize, MaxPartySize);

        return DefaultPartySize;
    }
}
