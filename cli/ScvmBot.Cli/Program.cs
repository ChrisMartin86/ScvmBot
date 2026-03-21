using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: scvmbot-cli generate [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  generate    Generate a MÖRK BORG character");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --name <name>      Character name override");
    Console.WriteLine("  --class <class>    Class name, or 'none' for classless");
    Console.WriteLine("  --roll <method>    3d6 (default) or 4d6-drop-lowest");
    Console.WriteLine("  --pdf <path>       Output a filled PDF character sheet to <path>");
    Console.WriteLine("  --data <path>      Path to game data directory");
    return;
}

if (!string.Equals(args[0], "generate", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    Console.Error.WriteLine("Run with --help for usage.");
    return;
}

// Parse options after the command
string? nameOverride = null;
string? className = null;
string? rollMethod = null;
string? dataPath = null;
string? pdfPath = null;

for (var i = 1; i < args.Length; i++)
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
        case "--pdf" when i + 1 < args.Length:
            pdfPath = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Unknown option: {args[i]}");
            return;
    }
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
var character = generator.Generate(options);

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
{
    Console.WriteLine($"  Items:     {string.Join(", ", character.Items)}");
}

if (character.ScrollsKnown.Count > 0)
{
    Console.WriteLine($"  Scrolls:   {string.Join(", ", character.ScrollsKnown)}");
}

foreach (var desc in character.Descriptions)
{
    Console.WriteLine($"  {desc.Category,-10}  {desc.Text}");
}

if (!string.IsNullOrWhiteSpace(character.Vignette))
{
    Console.WriteLine();
    Console.WriteLine($"  {character.Vignette}");
}

if (pdfPath is not null)
{
    var pdfRenderer = new MorkBorgPdfRenderer();
    if (!pdfRenderer.TemplateExists)
    {
        Console.Error.WriteLine("PDF template not found. Cannot generate PDF.");
        return;
    }

    var pdfBytes = pdfRenderer.Render(character);
    if (pdfBytes is null)
    {
        Console.Error.WriteLine("PDF rendering failed.");
        return;
    }

    File.WriteAllBytes(pdfPath, pdfBytes);
    Console.WriteLine();
    Console.WriteLine($"  PDF saved to {pdfPath}");
}
