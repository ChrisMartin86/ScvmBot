using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Cli;
using ScvmBot.Modules;

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
    CliHelpRenderer.PrintUsage(new Dictionary<string, IGameModule>());
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

    await CliBenchmarkRunner.RunAsync(module, subCommandDef.Name, benchOptions, cliOpts, ct);
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
