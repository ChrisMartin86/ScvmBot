namespace ScvmBot.Modules;

/// <summary>Discriminates option value types for command parameters.</summary>
public enum CommandOptionType
{
    String,
    Integer
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
    long? MaxValue = null);

/// <summary>Defines a subcommand within a game module's command group.</summary>
public sealed record SubCommandDefinition(
    string Name,
    string Description,
    IReadOnlyList<CommandOptionDefinition>? Options = null);
