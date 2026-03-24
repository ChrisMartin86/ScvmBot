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
    CliHelpRenderer.PrintUsage([]);
    Console.Error.WriteLine();
    Console.Error.WriteLine($"  (Module details unavailable: {ex.Message})");;
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
    CliHelpRenderer.PrintUsage(gameModules);
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
    // Benchmark generates one character per iteration for per-call timing.
    var benchOptions = new Dictionary<string, object?>(optionsDict, StringComparer.OrdinalIgnoreCase);
    benchOptions.Remove("count");
    benchOptions.Remove("name");

    if (cliOpts.Detailed)
    {
        var ticksPerGeneration = new long[cliOpts.Count];
        var sw = new Stopwatch();
        var startTime = DateTimeOffset.Now;
        var totalSw = Stopwatch.StartNew();

        for (var n = 0; n < cliOpts.Count; n++)
        {
            sw.Restart();
            await module.HandleGenerateCommandAsync(subCommandDef.Name, benchOptions, ct);
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
            await module.HandleGenerateCommandAsync(subCommandDef.Name, benchOptions, ct);

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
var result = await module.HandleGenerateCommandAsync(subCommandDef.Name, optionsDict, ct);
var card = registry.RenderCard(result);
CliHelpRenderer.PrintCard(card);

if (cliOpts.GeneratePdf)
{
    var pdfPath = cliOpts.PdfPath;
    var file = registry.TryRenderFile(result);
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

return 0;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Invalid option value: {ex.Message}");
    return 1;
}
