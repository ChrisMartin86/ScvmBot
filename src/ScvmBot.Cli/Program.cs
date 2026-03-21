using System.Diagnostics;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return;
}

if (!string.Equals(args[0], "generate", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    Console.Error.WriteLine("Run with --help for usage.");
    return;
}

if (args.Length < 2)
{
    Console.Error.WriteLine("Missing game system. Expected: scvmbot-cli generate morkborg character");
    return;
}

if (!string.Equals(args[1], "morkborg", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown game system: {args[1]}");
    Console.Error.WriteLine("Available: morkborg");
    return;
}

if (args.Length < 3)
{
    Console.Error.WriteLine("Missing subcommand. Expected: scvmbot-cli generate morkborg character");
    return;
}

if (!string.Equals(args[2], "character", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown subcommand: {args[2]}");
    Console.Error.WriteLine("Available: character");
    return;
}

// Parse options after "generate morkborg character"
string? nameOverride = null;
string? className = null;
string? rollMethod = null;
string? dataPath = null;
string? pdfPath = null;
bool generatePdf = false;
bool quiet = false;
bool detailed = false;
int count = 1;

for (var i = 3; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--name" when i + 1 < args.Length:
            nameOverride = args[++i];
            break;
        case "--class" when i + 1 < args.Length:
            className = args[++i];
            break;
        case "--roll" when i + 1 < args.Length:
            rollMethod = args[++i];
            break;
        case "--data" when i + 1 < args.Length:
            dataPath = args[++i];
            break;
        case "--count" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out count) || count < 1)
            {
                Console.Error.WriteLine("--count must be a positive integer.");
                return;
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
            // Next arg is the path if it exists and isn't another flag
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                pdfPath = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Unknown option: {args[i]}");
            return;
    }
}

if (!quiet && count > 20)
{
    Console.Error.WriteLine("--count maximum is 20 without --quiet mode.");
    return;
}

if (detailed && !quiet)
{
    Console.Error.WriteLine("--detailed requires --quiet.");
    return;
}

var refData = await MorkBorgReferenceDataService.CreateAsync(dataPath);
var generator = new CharacterGenerator(refData);

var options = new CharacterGenerationOptions
{
    Name = nameOverride,
    ClassName = className,
    RollMethod = string.Equals(rollMethod, "4d6-drop-lowest", StringComparison.OrdinalIgnoreCase)
        ? AbilityRollMethod.FourD6DropLowest
        : AbilityRollMethod.ThreeD6,
};

if (quiet)
{
    if (detailed)
    {
        var ticksPerGeneration = new long[count];
        var classCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        long totalHp = 0;
        long totalSilver = 0;
        var sw = new Stopwatch();
        var startTime = DateTimeOffset.Now;
        var totalSw = Stopwatch.StartNew();

        for (var n = 0; n < count; n++)
        {
            sw.Restart();
            var character = generator.Generate(options);
            sw.Stop();
            ticksPerGeneration[n] = sw.ElapsedTicks;
            totalHp += character.HitPoints;
            totalSilver += character.Silver;

            var key = character.ClassName ?? "Classless";
            classCounts.TryGetValue(key, out var c);
            classCounts[key] = c + 1;
        }

        totalSw.Stop();
        var endTime = DateTimeOffset.Now;

        Array.Sort(ticksPerGeneration);
        var tickFreq = (double)Stopwatch.Frequency;
        var minMs = ticksPerGeneration[0] / tickFreq * 1000;
        var maxMs = ticksPerGeneration[count - 1] / tickFreq * 1000;
        var medianMs = ticksPerGeneration[count / 2] / tickFreq * 1000;
        var avgMs = ticksPerGeneration.Average() / tickFreq * 1000;

        // P95 / P99
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
        Console.WriteLine();
        Console.WriteLine($"  Total HP:  {totalHp:N0}");
        Console.WriteLine($"  Avg HP:    {(double)totalHp / count:F2}");
        Console.WriteLine($"  Total Silver: {totalSilver:N0}");
        Console.WriteLine();
        Console.WriteLine("  Class distribution:");
        foreach (var (cls, cnt) in classCounts.OrderByDescending(kv => kv.Value))
            Console.WriteLine($"    {cls,-25} {cnt,8:N0}  ({100.0 * cnt / count:F1}%)");
    }
    else
    {
        long totalHp = 0;
        long totalSilver = 0;
        var startTime = DateTimeOffset.Now;
        var sw = Stopwatch.StartNew();

        for (var n = 0; n < count; n++)
        {
            var character = generator.Generate(options);
            totalHp += character.HitPoints;
            totalSilver += character.Silver;
        }

        sw.Stop();
        var endTime = DateTimeOffset.Now;

        Console.WriteLine($"  Started:   {startTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Finished:  {endTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
        Console.WriteLine($"  Count:     {count:N0}");
        Console.WriteLine($"  Elapsed:   {sw.Elapsed}");
        Console.WriteLine();
        Console.WriteLine($"  Total HP:  {totalHp:N0}");
        Console.WriteLine($"  Avg HP:    {(double)totalHp / count:F2}");
        Console.WriteLine($"  Total Silver: {totalSilver:N0}");
    }
    return;
}

var characters = new List<Character>(count);
for (var n = 0; n < count; n++)
{
    // Only apply name override to the first character when generating multiples,
    // so each subsequent character gets a random name.
    var charOptions = n == 0
        ? options
        : new CharacterGenerationOptions
        {
            ClassName = options.ClassName,
            RollMethod = options.RollMethod,
        };
    characters.Add(generator.Generate(charOptions));
}

for (var n = 0; n < characters.Count; n++)
{
    if (n > 0) Console.WriteLine(new string('-', 40));
    PrintCharacter(characters[n]);
}

if (generatePdf)
{
    var pdfRenderer = new MorkBorgPdfRenderer();
    if (!pdfRenderer.TemplateExists)
    {
        Console.Error.WriteLine("PDF template not found. Cannot generate PDF.");
        return;
    }

    if (characters.Count == 1)
    {
        pdfPath ??= SanitizeFileName(characters[0].Name) + ".pdf";
        var pdfBytes = pdfRenderer.Render(characters[0]);
        if (pdfBytes is null)
        {
            Console.Error.WriteLine("PDF rendering failed.");
            return;
        }

        File.WriteAllBytes(pdfPath, pdfBytes);
        Console.WriteLine();
        Console.WriteLine($"  PDF saved to {pdfPath}");
    }
    else
    {
        var memberPdfs = new List<(string CharacterName, byte[] PdfBytes)>();
        foreach (var character in characters)
        {
            var pdfBytes = pdfRenderer.Render(character);
            if (pdfBytes is null)
            {
                Console.Error.WriteLine($"PDF rendering failed for '{character.Name}'; skipping.");
                continue;
            }
            memberPdfs.Add((character.Name, pdfBytes));
        }

        if (memberPdfs.Count == 0)
        {
            Console.Error.WriteLine("All PDF renders failed.");
            return;
        }

        pdfPath ??= "characters.zip";
        var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
        File.WriteAllBytes(pdfPath, zipBytes);
        Console.WriteLine();
        Console.WriteLine($"  ZIP saved to {pdfPath} ({memberPdfs.Count} character sheets)");
    }
}

static void PrintCharacter(Character character)
{
    Console.WriteLine($"  Name:      {character.Name}");
    Console.WriteLine($"  Class:     {character.ClassName ?? "Classless"}");
    Console.WriteLine($"  HP:        {character.HitPoints}/{character.MaxHitPoints}");
    Console.WriteLine($"  Omens:     {character.Omens}");
    Console.WriteLine($"  Silver:    {character.Silver}s");
    Console.WriteLine();
    Console.WriteLine($"  STR {character.Strength,2}  AGI {character.Agility,2}  PRE {character.Presence,2}  TOU {character.Toughness,2}");
    Console.WriteLine();

    if (!string.IsNullOrWhiteSpace(character.EquippedWeapon))
        Console.WriteLine($"  Weapon:    {character.EquippedWeapon}");
    if (!string.IsNullOrWhiteSpace(character.EquippedArmor))
        Console.WriteLine($"  Armor:     {character.EquippedArmor}");

    if (character.Items.Count > 0)
        Console.WriteLine($"  Items:     {string.Join(", ", character.Items)}");

    if (character.ScrollsKnown.Count > 0)
        Console.WriteLine($"  Scrolls:   {string.Join(", ", character.ScrollsKnown)}");

    foreach (var desc in character.Descriptions)
        Console.WriteLine($"  {desc.Category,-10}  {desc.Text}");

    if (!string.IsNullOrWhiteSpace(character.Vignette))
    {
        Console.WriteLine();
        Console.WriteLine($"  {character.Vignette}");
    }
}

static string SanitizeFileName(string name)
{
    if (string.IsNullOrWhiteSpace(name)) return "character";
    var sanitized = new string(name
        .Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_')
        .ToArray())
        .Trim('_');
    return string.IsNullOrEmpty(sanitized) ? "character" : sanitized;
}

static void PrintUsage()
{
    Console.WriteLine("Usage: scvmbot-cli generate <game> <subcommand> [options]");
    Console.WriteLine();
    Console.WriteLine("Games:");
    Console.WriteLine("  morkborg              MÖRK BORG");
    Console.WriteLine();
    Console.WriteLine("Subcommands:");
    Console.WriteLine("  character              Generate a character");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --name <name>          Character name override");
    Console.WriteLine("  --class <class>        Class name, or 'none' for classless");
    Console.WriteLine("  --roll <method>        3d6 (default) or 4d6-drop-lowest");
    Console.WriteLine("  --count <n>            Number of characters to generate (1-20, default: 1)");
    Console.WriteLine("  --quiet                Benchmark mode: no output, prints timing only");
    Console.WriteLine("                         Removes the --count upper limit");
    Console.WriteLine("  --detailed             With --quiet: per-generation timing stats and");
    Console.WriteLine("                         class distribution summary");
    Console.WriteLine("  --pdf [path]           Output a filled PDF (or ZIP when count > 1)");
    Console.WriteLine("  --data <path>          Path to game data directory");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  scvmbot-cli generate morkborg character");
    Console.WriteLine("  scvmbot-cli generate morkborg character --class none --pdf");
    Console.WriteLine("  scvmbot-cli generate morkborg character --name Karg --roll 4d6-drop-lowest --pdf karg.pdf");
    Console.WriteLine("  scvmbot-cli generate morkborg character --count 4 --pdf party.zip");
    Console.WriteLine("  scvmbot-cli generate morkborg character --quiet --count 10000");
    Console.WriteLine("  scvmbot-cli generate morkborg character --quiet --detailed --count 10000");
}
