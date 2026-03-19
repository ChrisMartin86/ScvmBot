using Discord;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>
/// Parses Discord slash command options for party generation.
/// Extracts the optional "size" parameter from the party subcommand.
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

        // Try to parse as integer
        if (sizeOption.Value is long longValue)
        {
            var size = (int)longValue;
            return Math.Clamp(size, MinPartySize, MaxPartySize);
        }

        if (int.TryParse(sizeOption.Value.ToString(), out int parsedSize))
        {
            return Math.Clamp(parsedSize, MinPartySize, MaxPartySize);
        }

        return DefaultPartySize;
    }

    /// <summary>
    /// Parses party subcommand options to extract the optional party name.
    /// Returns null if party name is not specified.
    /// </summary>
    public static string? ParsePartyName(IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandGroupOptions)
    {
        if (subCommandGroupOptions == null || subCommandGroupOptions.Count == 0)
            return null;

        // Find the "party" SubCommand by type AND name
        var partySubcommand = subCommandGroupOptions
            .FirstOrDefault(o =>
                o.Type == ApplicationCommandOptionType.SubCommand &&
                string.Equals(o.Name, "party", StringComparison.OrdinalIgnoreCase));

        if (partySubcommand?.Options == null || partySubcommand.Options.Count == 0)
            return null;

        // Look for the "name" option
        var nameOption = partySubcommand.Options
            .FirstOrDefault(o => string.Equals(o.Name, "name", StringComparison.OrdinalIgnoreCase));

        if (nameOption?.Value == null)
            return null;

        var nameValue = nameOption.Value.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(nameValue) ? null : nameValue;
    }
}
