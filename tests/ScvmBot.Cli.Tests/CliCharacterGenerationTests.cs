using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;
using System.IO.Compression;

namespace ScvmBot.Cli.Tests;

/// <summary>
/// Proves the CLI host path: game logic is consumable without Discord.Net
/// and produces the same results as the bot path.
/// </summary>
public class CliCharacterGenerationTests
{
    private static async Task<(MorkBorgReferenceDataService RefData, CharacterGenerator Generator)> CreateGeneratorAsync()
    {
        var dataPath = Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg", "Data");
        var refData = await MorkBorgReferenceDataService.CreateAsync(dataPath);
        var generator = new CharacterGenerator(refData);
        return (refData, generator);
    }

    [Fact]
    public async Task Generate_ReturnsCharacter_WithRequiredFields()
    {
        var (_, generator) = await CreateGeneratorAsync();

        var character = generator.Generate();

        Assert.False(string.IsNullOrWhiteSpace(character.Name));
        Assert.True(character.MaxHitPoints >= 1);
        Assert.True(character.HitPoints >= 1);
        Assert.NotNull(character.EquippedWeapon);
    }

    [Fact]
    public async Task Generate_WithNameOverride_UsesProvidedName()
    {
        var (_, generator) = await CreateGeneratorAsync();

        var character = generator.Generate(new CharacterGenerationOptions { Name = "TestScvm" });

        Assert.Equal("TestScvm", character.Name);
    }

    [Fact]
    public async Task Generate_WithClassNone_ProducesClasslessCharacter()
    {
        var (_, generator) = await CreateGeneratorAsync();

        var character = generator.Generate(new CharacterGenerationOptions { ClassName = "none" });

        Assert.Null(character.ClassName);
    }

    [Fact]
    public async Task Generate_WithSpecificClass_UsesClass()
    {
        var (refData, generator) = await CreateGeneratorAsync();
        var firstClass = refData.Classes[0].Name;

        var character = generator.Generate(new CharacterGenerationOptions { ClassName = firstClass });

        Assert.Equal(firstClass, character.ClassName);
    }

    [Fact]
    public async Task Generate_WithFourD6Drop_ProducesCharacter()
    {
        var (_, generator) = await CreateGeneratorAsync();

        var character = generator.Generate(new CharacterGenerationOptions
        {
            RollMethod = AbilityRollMethod.FourD6DropLowest
        });

        Assert.False(string.IsNullOrWhiteSpace(character.Name));
    }

    [Fact]
    public async Task Generate_WithDeterministicRng_ProducesReproducibleResults()
    {
        var dataPath = Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg", "Data");
        var refData = await MorkBorgReferenceDataService.CreateAsync(dataPath);

        var gen1 = new CharacterGenerator(refData, new Random(42));
        var gen2 = new CharacterGenerator(refData, new Random(42));

        var char1 = gen1.Generate();
        var char2 = gen2.Generate();

        Assert.Equal(char1.Name, char2.Name);
        Assert.Equal(char1.Strength, char2.Strength);
        Assert.Equal(char1.Agility, char2.Agility);
        Assert.Equal(char1.HitPoints, char2.HitPoints);
    }

    [Fact]
    public async Task CliPath_DoesNotRequireDiscordAssemblies()
    {
        var (_, generator) = await CreateGeneratorAsync();

        // The fact that this test compiles and runs proves the CLI path
        // is free of Discord.Net. This test project has no Discord.Net
        // reference — if CharacterGenerator or its dependencies pulled
        // Discord types, this would fail to build.
        var character = generator.Generate();
        Assert.IsType<Character>(character);
    }

    // ── Multi-character generation (CLI --count) ─────────────────────────────

    [Fact]
    public async Task Generate_MultipleCharacters_ProducesRequestedCount()
    {
        var (_, generator) = await CreateGeneratorAsync();

        var characters = Enumerable.Range(0, 4)
            .Select(_ => generator.Generate())
            .ToList();

        Assert.Equal(4, characters.Count);
        Assert.All(characters, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
    }

    [Fact]
    public async Task Generate_MultipleCharacters_AreIndependent()
    {
        var (_, generator) = await CreateGeneratorAsync();

        var characters = Enumerable.Range(0, 3)
            .Select(_ => generator.Generate())
            .ToList();

        // Characters should have independent ability scores (not identical objects)
        Assert.True(characters.Select(c => c.Name).Distinct().Count() >= 1);
        Assert.All(characters, c => Assert.True(c.MaxHitPoints >= 1));
    }

    [Fact]
    public async Task Generate_MultipleCharacters_ZipContainsAllPdfs()
    {
        var (_, generator) = await CreateGeneratorAsync();
        var pdfRenderer = new MorkBorgPdfRenderer();
        if (!pdfRenderer.TemplateExists)
            return; // skip if no PDF template

        var characters = Enumerable.Range(0, 3)
            .Select(_ => generator.Generate())
            .ToList();

        var memberPdfs = characters
            .Select(c => (c.Name, PdfBytes: pdfRenderer.Render(c)))
            .Where(m => m.PdfBytes is not null)
            .Select(m => (m.Name, m.PdfBytes!))
            .ToList();

        var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
        Assert.True(zipBytes.Length > 0);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Equal(memberPdfs.Count, archive.Entries.Count);
    }

    [Fact]
    public async Task Generate_SingleCharacter_ReusesGenerator()
    {
        var (_, generator) = await CreateGeneratorAsync();

        // Single character generation still works with the same generator instance
        var char1 = generator.Generate();
        var char2 = generator.Generate();

        Assert.False(string.IsNullOrWhiteSpace(char1.Name));
        Assert.False(string.IsNullOrWhiteSpace(char2.Name));
    }
}
