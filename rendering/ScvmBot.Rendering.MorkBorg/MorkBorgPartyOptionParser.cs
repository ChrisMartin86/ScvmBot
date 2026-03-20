using Discord;

namespace ScvmBot.Rendering.MorkBorg;

/// <summary>
/// Parses Discord slash command options for party generation.
/// Extracts the optional "size" parameter from the party subcommand.
/// Party names are always generated randomly; there is no user-supplied name option.
/// </summary>
public sealed class MorkBorgPartyOptionParser
{
    private const int DefaultPartySize = 4;
    private const int MinPartySize = 1;
    private const int MaxPartySize = 4;

    /// <summary>
    /// Parses party subcommand options to extract party size.
    /// Returns <see cref="DefaultPartySize"/> if size is not specified or invalid.
    /// Clamps the size to <see cref="MinPartySize"/> - <see cref="MaxPartySize"/>.
    /// </summary>
    public static int ParsePartySize(IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandGroupOptions)
    {
        if (subCommandGroupOptions == null || subCommandGroupOptions.Count == 0)
            return DefaultPartySize;

        // Find the "party" SubCommand by type AND name
        var partySubcommand = subCommandGroupOptions
            .FirstOrDefault(o =>
                o.Type == ApplicationCommandOptionType.SubCommand &&
                string.Equals(o.Name, "party", StringComparison.OrdinalIgnoreCase));

        if (partySubcommand?.Options == null || partySubcommand.Options.Count == 0)
            return DefaultPartySize;

        // Look for the "size" option
        var sizeOption = partySubcommand.Options
            .FirstOrDefault(o => string.Equals(o.Name, "size", StringComparison.OrdinalIgnoreCase));

        if (sizeOption?.Value == null)
            return DefaultPartySize;

        if (sizeOption.Value is long longValue)
            return Math.Clamp((int)longValue, MinPartySize, MaxPartySize);

        return DefaultPartySize;
    }
}
