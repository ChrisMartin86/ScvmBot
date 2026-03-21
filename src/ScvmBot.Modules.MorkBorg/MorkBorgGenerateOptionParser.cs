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
    /// Pure parser testable without any transport types.
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
