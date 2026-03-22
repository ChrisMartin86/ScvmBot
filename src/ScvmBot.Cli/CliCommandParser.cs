using ScvmBot.Modules;

namespace ScvmBot.Cli;

/// <summary>
/// Extracts the <c>--data</c> path from raw CLI args (before module discovery)
/// and parses the remaining args into a structured <see cref="CliCommand"/>
/// once modules are available. Centralises all cross-cutting option handling
/// so new CLI flags are added here, not scattered through Program.cs.
/// </summary>
internal static class CliCommandParser
{
    /// <summary>
    /// Separates the <c>--data &lt;path&gt;</c> flag from the rest of the arguments.
    /// Must run before module discovery because the data path influences initialisation.
    /// </summary>
    public static (string? DataPath, string[] RemainingArgs) ExtractDataPath(string[] args)
    {
        string? dataPath = null;
        var remaining = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--data" && i + 1 < args.Length)
                dataPath = args[++i];
            else
                remaining.Add(args[i]);
        }
        return (dataPath, remaining.ToArray());
    }

    /// <summary>
    /// Parses post-discovery args into a <see cref="CliCommand"/>.
    /// Throws <see cref="CliParseException"/> with a user-friendly message on any parse error.
    /// </summary>
    public static CliCommand Parse(
        string[] commandArgs,
        IReadOnlyDictionary<string, IGameModule> modules)
    {
        if (!string.Equals(commandArgs[0], "generate", StringComparison.OrdinalIgnoreCase))
            throw new CliParseException($"Unknown command: {commandArgs[0]}\nRun with --help for usage.");

        if (commandArgs.Length < 2)
            throw new CliParseException("Missing game system. Available: " + string.Join(", ", modules.Keys));

        if (!modules.TryGetValue(commandArgs[1], out var module))
            throw new CliParseException($"Unknown game system: {commandArgs[1]}\nAvailable: " + string.Join(", ", modules.Keys));

        if (commandArgs.Length < 3)
        {
            var names = string.Join(", ", module.SubCommands.Select(sc => sc.Name));
            throw new CliParseException($"Missing subcommand. Available: {names}");
        }

        var subCommandDef = module.SubCommands
            .FirstOrDefault(sc => string.Equals(sc.Name, commandArgs[2], StringComparison.OrdinalIgnoreCase));

        if (subCommandDef is null)
        {
            var names = string.Join(", ", module.SubCommands.Select(sc => sc.Name));
            throw new CliParseException($"Unknown subcommand: {commandArgs[2]}\nAvailable: {names}");
        }

        var (moduleOptions, cliOptions) = ParseOptions(commandArgs, subCommandDef);
        return new CliCommand(module, subCommandDef, moduleOptions, cliOptions);
    }

    private static (IReadOnlyDictionary<string, object?> ModuleOptions, CliOptions CliOptions) ParseOptions(
        string[] commandArgs,
        SubCommandDefinition subCommandDef)
    {
        var moduleOptionNames = new HashSet<string>(
            subCommandDef.Options?.Select(o => o.Name) ?? [],
            StringComparer.OrdinalIgnoreCase);

        var moduleOptions = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        int count = 1;
        bool quiet = false;
        bool detailed = false;
        bool generatePdf = false;
        string? pdfPath = null;

        for (var i = 3; i < commandArgs.Length; i++)
        {
            switch (commandArgs[i])
            {
                case "--count" when i + 1 < commandArgs.Length:
                    if (!int.TryParse(commandArgs[++i], out count) || count < 1)
                        throw new CliParseException("--count must be a positive integer.");
                    break;
                case "--quiet":
                    quiet = true;
                    break;
                case "--detailed":
                    detailed = true;
                    break;
                case "--pdf":
                    generatePdf = true;
                    if (i + 1 < commandArgs.Length && !commandArgs[i + 1].StartsWith("--"))
                        pdfPath = commandArgs[++i];
                    break;
                default:
                {
                    var optName = commandArgs[i].TrimStart('-');
                    if (moduleOptionNames.Contains(optName))
                    {
                        if (i + 1 >= commandArgs.Length)
                            throw new CliParseException($"Missing value for --{optName}");

                        var optDef = subCommandDef.Options!
                            .First(o => string.Equals(o.Name, optName, StringComparison.OrdinalIgnoreCase));
                        var rawValue = commandArgs[++i];
                        moduleOptions[optDef.Name] = optDef.Type == CommandOptionType.Integer
                            && long.TryParse(rawValue, out var longVal)
                                ? longVal
                                : rawValue;
                    }
                    else
                    {
                        throw new CliParseException($"Unknown option: {commandArgs[i]}");
                    }
                    break;
                }
            }
        }

        if (!quiet && count > 20)
            throw new CliParseException("--count maximum is 20 without --quiet mode.");

        if (detailed && !quiet)
            throw new CliParseException("--detailed requires --quiet.");

        var cliOptions = new CliOptions(count, quiet, detailed, generatePdf, pdfPath);
        return (moduleOptions, cliOptions);
    }
}

/// <summary>
/// Thrown when CLI argument parsing fails. Message is suitable for display to the user.
/// </summary>
internal sealed class CliParseException(string message) : Exception(message);
