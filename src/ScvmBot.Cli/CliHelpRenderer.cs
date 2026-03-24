using ScvmBot.Modules;

namespace ScvmBot.Cli;

/// <summary>
/// Formats and writes help text and character card output to the console.
/// Owns all presentation logic that was previously embedded in Program.cs.
/// </summary>
internal static class CliHelpRenderer
{
    internal static void PrintCard(CardOutput card)
    {
        if (card.Title is not null)
            Console.WriteLine($"  {card.Title}");
        if (card.Description is not null)
            Console.WriteLine($"  {card.Description}");
        Console.WriteLine();

        if (card.Fields is not null)
        {
            foreach (var field in card.Fields)
            {
                var lines = field.Value.Split('\n');
                Console.WriteLine($"  {field.Name,-12} {lines[0]}");
                foreach (var line in lines.Skip(1))
                    Console.WriteLine($"               {line}");
            }
        }
    }

    internal static void PrintUsage(IReadOnlyDictionary<string, IGameModule> gameModules)
    {
        Console.WriteLine("Usage: scvmbot-cli [--data <path>] generate <game> <subcommand> [options]");
        Console.WriteLine();

        Console.WriteLine("Games:");
        if (gameModules.Count == 0)
        {
            Console.WriteLine("  (no modules discovered)");
        }
        else
        {
            foreach (var (key, mod) in gameModules.OrderBy(kv => kv.Key))
                Console.WriteLine($"  {key,-20} {mod.Name}");
        }
        Console.WriteLine();

        foreach (var (key, mod) in gameModules.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"Subcommands ({key}):");
            foreach (var sc in mod.SubCommands)
                Console.WriteLine($"  {sc.Name,-20} {sc.Description}");
            Console.WriteLine();

            foreach (var sc in mod.SubCommands)
            {
                // Skip "count" — it's exposed as the CLI's --count option instead
                var displayOptions = sc.Options?.Where(o => !string.Equals(o.Name, "count", StringComparison.OrdinalIgnoreCase)).ToList();
                if (displayOptions is not { Count: > 0 }) continue;
                Console.WriteLine($"Options ({key} {sc.Name}):");
                foreach (var opt in displayOptions)
                {
                    var hint = opt.Type == CommandOptionType.Integer ? "<n>" : "<value>";
                    var label = $"--{opt.Name} {hint}";
                    Console.WriteLine($"  {label,-22}{opt.Description}");
                    if (opt.Choices is { Count: > 0 })
                    {
                        var values = string.Join(", ", opt.Choices.Select(c => c.Value));
                        Console.WriteLine($"  {"",22}Values: {values}");
                    }
                    if (opt.MinValue.HasValue || opt.MaxValue.HasValue)
                    {
                        var range = (opt.MinValue, opt.MaxValue) switch
                        {
                            (long min, long max) => $"Range: {min}-{max}",
                            (long min, null) => $"Min: {min}",
                            (null, long max) => $"Max: {max}",
                            _ => ""
                        };
                        Console.WriteLine($"  {"",22}{range}");
                    }
                }
                Console.WriteLine();
            }
        }

        Console.WriteLine("CLI options:");
        Console.WriteLine("  --count <n>            Number of characters to generate (1-20, default: 1)");
        Console.WriteLine("  --quiet                Benchmark mode: no output, prints timing only");
        Console.WriteLine("                         Removes the --count upper limit");
        Console.WriteLine("  --detailed             With --quiet: per-generation timing stats");
        Console.WriteLine("                         (min, max, avg, median, P95, P99)");
        Console.WriteLine("  --pdf [path]           Output a filled PDF or ZIP (format depends on result type)");
        Console.WriteLine("  --data <path>          Path to game data directory");
        Console.WriteLine();

        Console.WriteLine("Examples:");
        var first = gameModules.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)).FirstOrDefault();
        if (first.Value is not null)
        {
            foreach (var sc in first.Value.SubCommands)
            {
                Console.WriteLine($"  scvmbot-cli generate {first.Key} {sc.Name}");
                Console.WriteLine($"  scvmbot-cli generate {first.Key} {sc.Name} --pdf");
            }
            var exSub = first.Value.SubCommands.FirstOrDefault()?.Name ?? "subcommand";
            Console.WriteLine($"  scvmbot-cli generate {first.Key} {exSub} --count 4 --pdf");
            Console.WriteLine($"  scvmbot-cli generate {first.Key} {exSub} --quiet --count 10000");
            Console.WriteLine($"  scvmbot-cli generate {first.Key} {exSub} --quiet --detailed --count 10000");
        }
        else
        {
            Console.WriteLine("  scvmbot-cli generate <game> <subcommand>");
            Console.WriteLine("  scvmbot-cli generate <game> <subcommand> --pdf");
        }
    }
}
