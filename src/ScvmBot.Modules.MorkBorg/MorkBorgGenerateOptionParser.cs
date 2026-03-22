using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Parses command options into <see cref="CharacterGenerationOptions"/>.
/// Separates parsing from command definition and execution for testability.
/// </summary>
public sealed class MorkBorgGenerateOptionParser
{
    /// <summary>
    /// Parses a transport-agnostic options dictionary into character generation options.
    /// </summary>
    public static CharacterGenerationOptions Parse(IReadOnlyDictionary<string, object?> options)
    {
        return ParseRawOptions(
            rollMethod: options.TryGetValue("roll-method", out var rm) ? rm?.ToString() : null,
            className: options.TryGetValue("class", out var cls) ? cls?.ToString() : null,
            nameOverride: options.TryGetValue("name", out var name) ? name?.ToString() : null);
    }

    /// <summary>
    /// Parses the count option from the options dictionary.
    /// Returns 1 if not specified.
    /// </summary>
    public static int ParseCount(IReadOnlyDictionary<string, object?> options)
    {
        if (!options.TryGetValue("count", out var countValue) || countValue is null)
            return 1;

        if (countValue is IConvertible convertible)
        {
            try
            {
                var count = convertible.ToInt32(null);
                if (count < 1)
                    throw new ArgumentException($"Invalid count: '{countValue}'. Must be a positive integer.");
                return count;
            }
            catch (Exception ex) when (ex is OverflowException or FormatException or InvalidCastException)
            {
                throw new ArgumentException($"Invalid count: '{countValue}'. Must be a positive integer.", ex);
            }
        }

        throw new ArgumentException($"Invalid count: '{countValue}'. Must be a positive integer.");
    }

    /// <summary>
    /// Pure parser testable without any transport types.
    /// 
    /// ClassName interpretation:
    /// - "none" => explicitly classless
    /// - null/omitted => random 50/50 classless vs classed
    /// - any other string => exact class name lookup
    /// 
    /// RollMethod interpretation:
    /// - null/omitted => ThreeD6 (default)
    /// - "3d6" => ThreeD6
    /// - "4d6-drop-lowest" => FourD6DropLowest
    /// - anything else => ArgumentException
    /// </summary>
    public static CharacterGenerationOptions ParseRawOptions(
        string? rollMethod,
        string? className,
        string? nameOverride) =>
        new CharacterGenerationOptions
        {
            RollMethod = ParseRollMethod(rollMethod),
            ClassName = className,
            Name = nameOverride,
        };

    private static AbilityRollMethod ParseRollMethod(string? rollMethod)
    {
        if (rollMethod is null)
            return AbilityRollMethod.ThreeD6;

        if (string.Equals(rollMethod, MorkBorgCommandDefinition.Choice3D6, StringComparison.OrdinalIgnoreCase))
            return AbilityRollMethod.ThreeD6;

        if (string.Equals(rollMethod, MorkBorgCommandDefinition.ChoiceFourD6Drop, StringComparison.OrdinalIgnoreCase))
            return AbilityRollMethod.FourD6DropLowest;

        throw new ArgumentException(
            $"Invalid roll method: '{rollMethod}'. Valid values are '{MorkBorgCommandDefinition.Choice3D6}' and '{MorkBorgCommandDefinition.ChoiceFourD6Drop}'.");
    }
}
