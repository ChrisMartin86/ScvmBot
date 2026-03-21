using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Modules;
using System.Diagnostics;
using System.Reflection;

// ── Pre-parse --data (extracted before module discovery) ──────────────────
string? dataPath = null;
var filteredArgs = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--data" && i + 1 < args.Length)
        dataPath = args[++i];
    else
        filteredArgs.Add(args[i]);
}
var commandArgs = filteredArgs.ToArray();

if (commandArgs.Length == 0 || commandArgs[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

// ── Module discovery & initialization ────────────────────────────────────
List<IModuleRegistration> registrations;
try
{
    registrations = await DiscoverAndInitializeModulesAsync(dataPath);
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
foreach (var reg in registrations)
    reg.Register(services);
services.AddSingleton<RendererRegistry>();
services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
using var provider = services.BuildServiceProvider();

var gameModules = provider.GetServices<IGameModule>()
    .ToDictionary(m => m.CommandKey, StringComparer.OrdinalIgnoreCase);
var registry = provider.GetRequiredService<RendererRegistry>();

// ── Parse command structure ─────────────────────────────────────────────
if (!string.Equals(commandArgs[0], "generate", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command: {commandArgs[0]}");
    Console.Error.WriteLine("Run with --help for usage.");
    return 1;
}

if (commandArgs.Length < 2)
{
    Console.Error.WriteLine("Missing game system. Available: " + string.Join(", ", gameModules.Keys));
    return 1;
}

if (!gameModules.TryGetValue(commandArgs[1], out var module))
{
    Console.Error.WriteLine($"Unknown game system: {commandArgs[1]}");
    Console.Error.WriteLine("Available: " + string.Join(", ", gameModules.Keys));
    return 1;
}

if (commandArgs.Length < 3)
{
    var subNames = string.Join(", ", module.SubCommands.Select(sc => sc.Name));
    Console.Error.WriteLine($"Missing subcommand. Available: {subNames}");
    return 1;
}

var subCommandName = commandArgs[2];
var subCommandDef = module.SubCommands
    .FirstOrDefault(sc => string.Equals(sc.Name, subCommandName, StringComparison.OrdinalIgnoreCase));

if (subCommandDef is null)
{
    var subNames = string.Join(", ", module.SubCommands.Select(sc => sc.Name));
    Console.Error.WriteLine($"Unknown subcommand: {subCommandName}");
    Console.Error.WriteLine($"Available: {subNames}");
    return 1;
}

// ── Parse options ───────────────────────────────────────────────────────
var moduleOptionNames = new HashSet<string>(
    subCommandDef.Options?.Select(o => o.Name) ?? [],
    StringComparer.OrdinalIgnoreCase);

var optionsDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
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
            {
                Console.Error.WriteLine("--count must be a positive integer.");
                return 1;
            }
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
                {
                    Console.Error.WriteLine($"Missing value for --{optName}");
                    return 1;
                }
                var optDef = subCommandDef.Options!
                    .First(o => string.Equals(o.Name, optName, StringComparison.OrdinalIgnoreCase));
                var rawValue = commandArgs[++i];
                optionsDict[optDef.Name] = optDef.Type == CommandOptionType.Integer
                    && long.TryParse(rawValue, out var longVal)
                        ? longVal
                        : rawValue;
            }
            else
            {
                Console.Error.WriteLine($"Unknown option: {commandArgs[i]}");
                return 1;
            }
            break;
        }
    }
}

if (!quiet && count > 20)
{
    Console.Error.WriteLine("--count maximum is 20 without --quiet mode.");
    return 1;
}

if (detailed && !quiet)
{
    Console.Error.WriteLine("--detailed requires --quiet.");
    return 1;
}

// ── Benchmark mode ──────────────────────────────────────────────────────
if (quiet)
{
    if (detailed)
    {
        var ticksPerGeneration = new long[count];
        var sw = new Stopwatch();
        var startTime = DateTimeOffset.Now;
        var totalSw = Stopwatch.StartNew();

        for (var n = 0; n < count; n++)
        {
            sw.Restart();
            await module.HandleGenerateCommandAsync(subCommandDef.Name, optionsDict);
            sw.Stop();
            ticksPerGeneration[n] = sw.ElapsedTicks;
        }

        totalSw.Stop();
        var endTime = DateTimeOffset.Now;

        Array.Sort(ticksPerGeneration);
        var tickFreq = (double)Stopwatch.Frequency;
        var minMs = ticksPerGeneration[0] / tickFreq * 1000;
        var maxMs = ticksPerGeneration[count - 1] / tickFreq * 1000;
        var medianMs = ticksPerGeneration[count / 2] / tickFreq * 1000;
        var avgMs = ticksPerGeneration.Average() / tickFreq * 1000;
        var p95Ms = ticksPerGeneration[(int)(count * 0.95)] / tickFreq * 1000;
        var p99Ms = ticksPerGeneration[(int)(count * 0.99)] / tickFreq * 1000;

        Console.WriteLine($"  Started:   {startTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Finished:  {endTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Count:     {count:N0}");
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

        for (var n = 0; n < count; n++)
            await module.HandleGenerateCommandAsync(subCommandDef.Name, optionsDict);

        sw.Stop();
        var endTime = DateTimeOffset.Now;

        Console.WriteLine($"  Started:   {startTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Finished:  {endTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Count:     {count:N0}");
        Console.WriteLine($"  Elapsed:   {sw.Elapsed}");
    }
    return 0;
}

// ── Normal generation mode ──────────────────────────────────────────────
var results = new List<(GenerateResult Result, CardOutput Card)>(count);
for (var n = 0; n < count; n++)
{
    // Only apply name override to the first character when generating multiples
    var iterOptions = (n == 0 || !optionsDict.ContainsKey("name"))
        ? optionsDict
        : new Dictionary<string, object?>(
            optionsDict.Where(kv => !string.Equals(kv.Key, "name", StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

    var result = await module.HandleGenerateCommandAsync(subCommandDef.Name, iterOptions);
    var card = registry.RenderCard(result);
    results.Add((result, card));
}

for (var n = 0; n < results.Count; n++)
{
    if (n > 0) Console.WriteLine(new string('-', 40));
    PrintCard(results[n].Card);
}

if (generatePdf)
{
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
        Console.WriteLine($"  PDF saved to {pdfPath}");
    }
    else
    {
        var memberPdfs = new List<(string CharacterName, byte[] PdfBytes)>();
        foreach (var (result, card) in results)
        {
            var file = registry.TryRenderFile(result);
            if (file is not null)
                memberPdfs.Add((card.Title ?? "character", file.Bytes));
            else
                Console.Error.WriteLine($"PDF rendering failed for '{card.Title}'; skipping.");
        }

        if (memberPdfs.Count == 0)
        {
            Console.Error.WriteLine("All PDF renders failed.");
            return 1;
        }

        pdfPath ??= "characters.zip";
        var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
        File.WriteAllBytes(pdfPath, zipBytes);
        Console.WriteLine();
        Console.WriteLine($"  ZIP saved to {pdfPath} ({memberPdfs.Count} character sheets)");
    }
}

return 0;

// ── Helper methods ──────────────────────────────────────────────────────

static async Task<List<IModuleRegistration>> DiscoverAndInitializeModulesAsync(string? dataPath)
{
    var settings = new Dictionary<string, string>();
    if (dataPath is not null)
        settings["DataPath"] = dataPath;

    var baseDir = AppContext.BaseDirectory;
    var registrationTypes = Directory.GetFiles(baseDir, "ScvmBot.Modules.*.dll")
        .Select(Path.GetFileNameWithoutExtension)
        .Where(name => name is not null)
        .Select(name => { try { return Assembly.Load(name!); } catch { return null; } })
        .Where(a => a is not null)
        .SelectMany(a => a!.GetExportedTypes())
        .Where(t => typeof(IModuleRegistration).IsAssignableFrom(t)
                 && !t.IsAbstract
                 && !t.IsInterface);

    var modules = new List<IModuleRegistration>();
    foreach (var type in registrationTypes)
    {
        var mod = (IModuleRegistration)Activator.CreateInstance(type)!;
        if (settings.Count > 0)
            mod.Configure(settings);
        await mod.InitializeAsync();
        modules.Add(mod);
    }

    return modules;
}

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

static void PrintUsage()
{
    Console.WriteLine("Usage: scvmbot-cli [--data <path>] generate <game> <subcommand> [options]");
    Console.WriteLine();
    Console.WriteLine("Games:");
    Console.WriteLine("  morkborg              MÖRK BORG");
    Console.WriteLine();
    Console.WriteLine("Subcommands (morkborg):");
    Console.WriteLine("  character              Generate a random MÖRK BORG character");
    Console.WriteLine("  party                  Generate a full adventuring party");
    Console.WriteLine();
    Console.WriteLine("Module options (passed through to the game module):");
    Console.WriteLine("  --name <name>          Character name override");
    Console.WriteLine("  --class <class>        Class name, 'none' for classless, or omit for random");
    Console.WriteLine("  --roll-method <method> 3d6 (default) or 4d6-drop-lowest");
    Console.WriteLine("  --size <n>             Party size (1-4, default 4)");
    Console.WriteLine();
    Console.WriteLine("CLI options:");
    Console.WriteLine("  --count <n>            Number of characters to generate (1-20, default: 1)");
    Console.WriteLine("  --quiet                Benchmark mode: no output, prints timing only");
    Console.WriteLine("                         Removes the --count upper limit");
    Console.WriteLine("  --detailed             With --quiet: per-generation timing stats");
    Console.WriteLine("                         (min, max, avg, median, P95, P99)");
    Console.WriteLine("  --pdf [path]           Output a filled PDF (or ZIP when count > 1)");
    Console.WriteLine("  --data <path>          Path to game data directory");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  scvmbot-cli generate morkborg character");
    Console.WriteLine("  scvmbot-cli generate morkborg character --class none --pdf");
    Console.WriteLine("  scvmbot-cli generate morkborg character --name Karg --roll-method 4d6-drop-lowest --pdf karg.pdf");
    Console.WriteLine("  scvmbot-cli generate morkborg character --count 4 --pdf party.zip");
    Console.WriteLine("  scvmbot-cli generate morkborg party --size 3");
    Console.WriteLine("  scvmbot-cli generate morkborg party --size 3 --pdf");
    Console.WriteLine("  scvmbot-cli generate morkborg character --quiet --count 10000");
    Console.WriteLine("  scvmbot-cli generate morkborg character --quiet --detailed --count 10000");
}
