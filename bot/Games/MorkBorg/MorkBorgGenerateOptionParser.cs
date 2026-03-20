using Discord;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>
/// Parses Discord slash command options into <see cref="CharacterGenerationOptions"/>.
/// Separates parsing from command definition and execution for testability.
/// </summary>
public sealed class MorkBorgGenerateOptionParser
{
    /// <summary>
    /// Parses subcommand options from the Discord interaction.
    /// Throws InvalidOperationException if the expected "character" subcommand is not present.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the command structure is malformed.</exception>
    public static CharacterGenerationOptions Parse(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandGroupOptions)
    {
        if (subCommandGroupOptions == null || subCommandGroupOptions.Count == 0)
        {
            throw new InvalidOperationException(
                "No subcommand provided. Expected: /generate morkborg character [options]");
        }

        // Find the "character" SubCommand by type AND name, not by position.
        var subcommand = subCommandGroupOptions
            .FirstOrDefault(o =>
                o.Type == ApplicationCommandOptionType.SubCommand &&
                string.Equals(o.Name, "character", StringComparison.OrdinalIgnoreCase));

        if (subcommand == null)
        {
            throw new InvalidOperationException(
                "Expected 'character' subcommand was not provided.");
        }

        if (subcommand.Options == null)
            return ParseRawOptions(null, null, null);

        var opts = subcommand.Options;
        return ParseRawOptions(
            rollMethod: FindOptionValueByName(opts, "roll-method"),
            className: FindOptionValueByName(opts, "class"),
            nameOverride: FindOptionValueByName(opts, "name"));
    }

    private static string? FindOptionValueByName(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption> options,
        string optionName)
    {
        var option = options.FirstOrDefault(o => o.Name == optionName);
        return option?.Value?.ToString();
    }

    /// <summary>
    /// Pure parser testable without Discord types.
    /// 
    /// ClassName interpretation:
    /// - "none" => explicitly classless
    /// - null/omitted => random 50/50 classless vs classed
    /// - any other string => exact class name lookup
    /// </summary>
    public static CharacterGenerationOptions ParseRawOptions(
        string? rollMethod,
        string? className,
        string? nameOverride) =>
        new CharacterGenerationOptions
        {
            RollMethod = rollMethod == MorkBorgCommandDefinition.ChoiceFourD6Drop
                ? AbilityRollMethod.FourD6DropLowest
                : AbilityRollMethod.ThreeD6,
            ClassName = className,
            Name = nameOverride,
        };
}
