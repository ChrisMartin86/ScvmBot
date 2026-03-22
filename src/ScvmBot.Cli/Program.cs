using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Cli;
using ScvmBot.Modules;
using System.Diagnostics;

// ── Cancellation support (Ctrl+C) ───────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
var ct = cts.Token;

// ── Pre-parse --data (must happen before module discovery) ───────────────
var (dataPath, commandArgs) = CliCommandParser.ExtractDataPath(args);

bool showHelp = commandArgs.Length == 0 || commandArgs[0] is "-h" or "--help";

// ── Module discovery & initialization ────────────────────────────────────
var configPairs = new Dictionary<string, string?>();
if (dataPath is not null)
    configPairs["Modules:DataPath"] = dataPath;
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(configPairs)
    .Build();

List<Action<IServiceCollection>> registrations;
try
{
    registrations = await ModuleBootstrapper.DiscoverAndInitializeAsync(configuration);
}
catch (Exception ex) when (showHelp)
{
    PrintUsage([]);
    Console.Error.WriteLine();
    Console.Error.WriteLine($"  (Module details unavailable: {ex.Message})");
    return 0;
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine("[scvmbot-cli] Startup failed: a required data file is missing.");
    Console.Error.WriteLine($"  {ex.Message}");
    Console.Error.WriteLine("  Ensure the Data/ directory is present and contains all required JSON files.");
    return 1;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine("[scvmbot-cli] Startup failed: module could not be initialized.");
    Console.Error.WriteLine($"  {ex.Message}");
    return 1;
}

// ── Build DI container from discovered modules ──────────────────────────
var services = new ServiceCollection();
foreach (var register in registrations)
    register(services);
services.AddSingleton<RendererRegistry>();
services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
using var provider = services.BuildServiceProvider();

var gameModules = provider.GetServices<IGameModule>()
    .ToDictionary(m => m.CommandKey, StringComparer.OrdinalIgnoreCase);
var registry = provider.GetRequiredService<RendererRegistry>();

if (showHelp)
{
    PrintUsage(gameModules);
    return 0;
}

// ── Parse command ───────────────────────────────────────────────────────
CliCommand cmd;
try
{
    cmd = CliCommandParser.Parse(commandArgs, gameModules);
}
catch (CliParseException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

var module = cmd.Module;
var subCommandDef = cmd.SubCommand;
var optionsDict = cmd.ModuleOptions;
var cliOpts = cmd.Options;

try
{

// ── Benchmark mode ──────────────────────────────────────────────────────
if (cliOpts.Quiet)
{
    if (cliOpts.Detailed)
    {
        var ticksPerGeneration = new long[cliOpts.Count];
        var sw = new Stopwatch();
        var startTime = DateTimeOffset.Now;
        var totalSw = Stopwatch.StartNew();

        for (var n = 0; n < cliOpts.Count; n++)
        {
            sw.Restart();
            await module.HandleGenerateCommandAsync(subCommandDef.Name, optionsDict, ct);
            sw.Stop();
            ticksPerGeneration[n] = sw.ElapsedTicks;
        }

        totalSw.Stop();
        var endTime = DateTimeOffset.Now;

        Array.Sort(ticksPerGeneration);
        var tickFreq = (double)Stopwatch.Frequency;
        var minMs = ticksPerGeneration[0] / tickFreq * 1000;
        var maxMs = ticksPerGeneration[cliOpts.Count - 1] / tickFreq * 1000;
        var medianMs = ticksPerGeneration[cliOpts.Count / 2] / tickFreq * 1000;
        var avgMs = ticksPerGeneration.Average() / tickFreq * 1000;
        var p95Ms = ticksPerGeneration[(int)(cliOpts.Count * 0.95)] / tickFreq * 1000;
        var p99Ms = ticksPerGeneration[(int)(cliOpts.Count * 0.99)] / tickFreq * 1000;

        Console.WriteLine($"  Started:   {startTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Finished:  {endTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Count:     {cliOpts.Count:N0}");
        Console.WriteLine($"  Elapsed:   {totalSw.Elapsed}");
        Console.WriteLine();
        Console.WriteLine("  Per-generation timing:");
        Console.WriteLine($"    Min:     {minMs:F4} ms");
        Console.WriteLine($"    Max:     {maxMs:F4} ms");
        Console.WriteLine($"    Avg:     {avgMs:F4} ms");
        Console.WriteLine($"    Median:  {medianMs:F4} ms");
        Console.WriteLine($"    P95:     {p95Ms:F4} ms");
        Console.WriteLine($"    P99:     {p99Ms:F4} ms");
    }
    else
    {
        var startTime = DateTimeOffset.Now;
        var sw = Stopwatch.StartNew();

        for (var n = 0; n < cliOpts.Count; n++)
            await module.HandleGenerateCommandAsync(subCommandDef.Name, optionsDict, ct);

        sw.Stop();
        var endTime = DateTimeOffset.Now;

        Console.WriteLine($"  Started:   {startTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Finished:  {endTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Count:     {cliOpts.Count:N0}");
        Console.WriteLine($"  Elapsed:   {sw.Elapsed}");
    }
    return 0;
}

// ── Normal generation mode ──────────────────────────────────────────────
var results = new List<(GenerateResult Result, CardOutput Card)>(cliOpts.Count);
for (var n = 0; n < cliOpts.Count; n++)
{
    // Only apply name override to the first character when generating multiples
    var iterOptions = (n == 0 || !optionsDict.ContainsKey("name"))
        ? optionsDict
        : new Dictionary<string, object?>(
            optionsDict.Where(kv => !string.Equals(kv.Key, "name", StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

    var result = await module.HandleGenerateCommandAsync(subCommandDef.Name, iterOptions, ct);
    var card = registry.RenderCard(result);
    results.Add((result, card));
}

for (var n = 0; n < results.Count; n++)
{
    if (n > 0) Console.WriteLine(new string('-', 40));
    PrintCard(results[n].Card);
}

if (cliOpts.GeneratePdf)
{
    var pdfPath = cliOpts.PdfPath;
    if (results.Count == 1)
    {
        var file = registry.TryRenderFile(results[0].Result);
        if (file is null)
        {
            Console.Error.WriteLine("PDF rendering is not available.");
            return 1;
        }
        pdfPath ??= file.FileName;
        File.WriteAllBytes(pdfPath, file.Bytes);
        Console.WriteLine();
        Console.WriteLine($"  Saved to {pdfPath}");
    }
    else
    {
        var renderedFiles = new List<(string Name, byte[] Bytes, string FileName)>();
        foreach (var (result, card) in results)
        {
            var file = registry.TryRenderFile(result);
            if (file is not null)
                renderedFiles.Add((card.Title ?? "character", file.Bytes, file.FileName));
            else
                Console.Error.WriteLine($"File rendering failed for '{card.Title}'; skipping.");
        }

        if (renderedFiles.Count == 0)
        {
            Console.Error.WriteLine("All file renders failed.");
            return 1;
        }

        // If every rendered file is already a complete archive (e.g. party ZIPs),
        // write each one individually rather than wrapping ZIPs inside another ZIP.
        var allArchives = renderedFiles.All(f => f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        if (allArchives)
        {
            for (var i = 0; i < renderedFiles.Count; i++)
            {
                var outputName = renderedFiles.Count == 1
                    ? pdfPath ?? renderedFiles[i].FileName
                    : $"{Path.GetFileNameWithoutExtension(renderedFiles[i].FileName)}_{i + 1}.zip";
                File.WriteAllBytes(outputName, renderedFiles[i].Bytes);
                Console.WriteLine();
                Console.WriteLine($"  Saved to {outputName}");
            }
        }
        else
        {
            var memberPdfs = renderedFiles.Select(f => (f.Name, f.Bytes)).ToList();
            pdfPath ??= "characters.zip";
            var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
            File.WriteAllBytes(pdfPath, zipBytes);
            Console.WriteLine();
            Console.WriteLine($"  ZIP saved to {pdfPath} ({memberPdfs.Count} character sheets)");
        }
    }
}

return 0;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Invalid option value: {ex.Message}");
    return 1;
}

// ── Helper methods ──────────────────────────────────────────────────────

static void PrintCard(CardOutput card)
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

static void PrintUsage(Dictionary<string, IGameModule> gameModules)
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
            if (sc.Options is not { Count: > 0 }) continue;
            Console.WriteLine($"Options ({key} {sc.Name}):");
            foreach (var opt in sc.Options)
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
