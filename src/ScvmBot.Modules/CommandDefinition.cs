namespace ScvmBot.Modules;

/// <summary>Discriminates option value types for command parameters.</summary>
public enum CommandOptionType
{
    String,
    Integer
}

/// <summary>
/// Semantic role of a command option, used by transport hosts (e.g. Discord, CLI)
/// to apply transport-specific constraints without hard-coding option names.
/// <para>
/// Hosts match on role rather than name so module-defined options survive renames.
/// Add new roles sparingly — each one is a contract between modules and every host.
/// </para>
/// </summary>
public enum CommandOptionRole
{
    None,
    GenerationCount
}

/// <summary>A named choice presented to the user for a command option.</summary>
public sealed record CommandChoice(string Label, string Value);

/// <summary>Defines a single option within a subcommand.</summary>
public sealed record CommandOptionDefinition(
    string Name,
    string Description,
    CommandOptionType Type,
    bool Required = false,
    IReadOnlyList<CommandChoice>? Choices = null,
    long? MinValue = null,
    long? MaxValue = null,
    CommandOptionRole Role = CommandOptionRole.None);

/// <summary>Defines a subcommand within a game module's command group.</summary>
public sealed record SubCommandDefinition(
    string Name,
    string Description,
    IReadOnlyList<CommandOptionDefinition>? Options = null);
