using ScvmBot.Games.CyBorg.Models;

namespace ScvmBot.Modules.CyBorg;

/// <summary>
/// Parses command options into <see cref="CyBorgCharacterGenerationOptions"/>.
/// </summary>
public sealed class CyBorgGenerateOptionParser
{
    /// <summary>
    /// Parses a transport-agnostic options dictionary into character generation options.
    /// </summary>
    public static CyBorgCharacterGenerationOptions Parse(IReadOnlyDictionary<string, object?> options)
    {
        return ParseRawOptions(
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
    /// </summary>
    public static CyBorgCharacterGenerationOptions ParseRawOptions(
        string? className,
        string? nameOverride) =>
        new CyBorgCharacterGenerationOptions
        {
            ClassName = className,
            Name = nameOverride,
        };
}
