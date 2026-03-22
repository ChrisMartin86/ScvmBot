using ScvmBot.Modules;

namespace ScvmBot.Cli;

/// <summary>Parsed CLI command with resolved module, subcommand, and all options.</summary>
internal sealed record CliCommand(
    IGameModule Module,
    SubCommandDefinition SubCommand,
    IReadOnlyDictionary<string, object?> ModuleOptions,
    CliOptions Options);

/// <summary>Cross-cutting CLI options independent of game modules.</summary>
internal sealed record CliOptions(
    int Count = 1,
    bool Quiet = false,
    bool Detailed = false,
    bool GeneratePdf = false,
    string? PdfPath = null);
