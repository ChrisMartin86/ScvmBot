using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Cli.Tests;

/// <summary>
/// Proves the CLI host path: generation and rendering flow through the shared
/// module pipeline (IGameModule + RendererRegistry) without Discord.Net.
/// </summary>
public class CliCharacterGenerationTests
{
    private static async Task<(IGameModule Module, RendererRegistry Registry)> CreateModulePipelineAsync()
    {
        var dataPath = Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg", "Data");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Modules:MorkBorg:DataPath"] = dataPath })
            .Build();

        var register = await new MorkBorgModuleRegistration().InitializeAsync(config);

        var services = new ServiceCollection();
        register(services);
        services.AddSingleton<RendererRegistry>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<IGameModule>(), provider.GetRequiredService<RendererRegistry>());
    }

    [Fact]
    public async Task Generate_ReturnsCharacter_WithRequiredFields()
    {
        var (module, _) = await CreateModulePipelineAsync();

        var result = await module.HandleGenerateCommandAsync("character", new Dictionary<string, object?>(), TestContext.Current.CancellationToken);

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.False(string.IsNullOrWhiteSpace(charResult.Characters[0].Name));
        Assert.True(charResult.Characters[0].MaxHitPoints >= 1);
        Assert.True(charResult.Characters[0].HitPoints >= 1);
        Assert.NotNull(charResult.Characters[0].EquippedWeapon);
    }

    [Fact]
    public async Task Generate_WithNameOverride_UsesProvidedName()
    {
        var (module, _) = await CreateModulePipelineAsync();
        var options = new Dictionary<string, object?> { ["name"] = "TestScvm" };

        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Equal("TestScvm", charResult.Characters[0].Name);
    }

    [Fact]
    public async Task Generate_WithClassNone_ProducesClasslessCharacter()
    {
        var (module, _) = await CreateModulePipelineAsync();
        var options = new Dictionary<string, object?> { ["class"] = "none" };

        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Null(charResult.Characters[0].ClassName);
    }

    [Fact]
    public async Task Generate_WithSpecificClass_UsesClass()
    {
        var (module, _) = await CreateModulePipelineAsync();
        // "character" subcommand with a class name from the module's SubCommands
        var classChoice = module.SubCommands
            .First(sc => sc.Name == "character")
            .Options!.First(o => o.Name == "class")
            .Choices!.First(c => c.Value != "none");
        var options = new Dictionary<string, object?> { ["class"] = classChoice.Value };

        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Equal(classChoice.Value, charResult.Characters[0].ClassName);
    }

    [Fact]
    public async Task Generate_WithFourD6Drop_Classless_ProducesDifferentAbilities_ThanThreeD6()
    {
        var (module, _) = await CreateModulePipelineAsync();

        // Generate many classless characters with each roll method using the same seed range.
        // The heroic roll path (4d6 drop lowest on 2 random abilities) should produce
        // statistically different ability distributions than straight 3d6.
        var threeD6Abilities = new List<(int S, int A, int P, int T)>();
        var fourD6Abilities = new List<(int S, int A, int P, int T)>();

        for (int seed = 0; seed < 20; seed++)
        {
            var gen3 = ScvmBot.Games.MorkBorg.Generation.CharacterGeneratorFactory.Create(
                await ScvmBot.Games.MorkBorg.Reference.MorkBorgReferenceDataService.CreateAsync(
                    Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg", "Data")),
                new Random(seed));
            var ch3 = gen3.Generate(new CharacterGenerationOptions
            {
                ClassName = "none",
                RollMethod = AbilityRollMethod.ThreeD6
            });
            threeD6Abilities.Add((ch3.Strength, ch3.Agility, ch3.Presence, ch3.Toughness));

            var gen4 = ScvmBot.Games.MorkBorg.Generation.CharacterGeneratorFactory.Create(
                await ScvmBot.Games.MorkBorg.Reference.MorkBorgReferenceDataService.CreateAsync(
                    Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg", "Data")),
                new Random(seed));
            var ch4 = gen4.Generate(new CharacterGenerationOptions
            {
                ClassName = "none",
                RollMethod = AbilityRollMethod.FourD6DropLowest
            });
            fourD6Abilities.Add((ch4.Strength, ch4.Agility, ch4.Presence, ch4.Toughness));
        }

        // At least some characters must have different abilities, proving the roll method matters
        var differences = threeD6Abilities.Zip(fourD6Abilities, (a, b) => a != b).Count(d => d);
        Assert.True(differences > 0,
            "4d6-drop-lowest should produce different ability scores than 3d6 for classless characters.");
    }

    // ── Rendering through RendererRegistry ───────────────────────────────

    [Fact]
    public async Task RenderCard_ReturnsCardOutput_ForCharacterResult()
    {
        var (module, registry) = await CreateModulePipelineAsync();
        var result = await module.HandleGenerateCommandAsync("character", new Dictionary<string, object?>(), TestContext.Current.CancellationToken);

        var card = registry.RenderCard(result);

        Assert.NotNull(card.Title);
        Assert.NotNull(card.Description);
        Assert.NotNull(card.Fields);
        Assert.True(card.Fields.Count > 0);
    }

    [Fact]
    public async Task TryRenderFile_ReturnsFileOutput_ForCharacterResult()
    {
        var (module, registry) = await CreateModulePipelineAsync();
        var result = await module.HandleGenerateCommandAsync("character", new Dictionary<string, object?>(), TestContext.Current.CancellationToken);

        var file = registry.TryRenderFile(result);

        // PDF template may not be available in all environments
        if (file is not null)
        {
            Assert.True(file.Bytes.Length > 0);
            Assert.EndsWith(".pdf", file.FileName);
        }
    }

    [Fact]
    public async Task Generate_MultipleCharacters_ZipContainsAllPdfs()
    {
        var (module, registry) = await CreateModulePipelineAsync();
        var options = new Dictionary<string, object?> { ["count"] = 3L };

        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);
        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Equal(3, charResult.Characters.Count);

        var file = registry.TryRenderFile(result);
        if (file is null)
            return; // skip if no PDF template

        Assert.EndsWith(".zip", file.FileName);
        Assert.True(file.Bytes.Length > 0);

        using var stream = new MemoryStream(file.Bytes);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        Assert.Equal(3, archive.Entries.Count);
        Assert.All(archive.Entries, e => Assert.EndsWith(".pdf", e.Name));
    }

    // ── Multi-character generation through module pipeline ─────────────────────────

    [Fact]
    public async Task GenerateMulti_ReturnsMultiCharacterResult()
    {
        var (module, _) = await CreateModulePipelineAsync();
        var options = new Dictionary<string, object?> { ["count"] = 3L };

        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Equal(3, charResult.Characters.Count);
    }

    [Fact]
    public async Task GenerateMulti_RenderCard_ReturnsCardOutput()
    {
        var (module, registry) = await CreateModulePipelineAsync();
        var options = new Dictionary<string, object?> { ["count"] = 2L };

        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);
        var card = registry.RenderCard(result);

        Assert.NotNull(card.Title);
        Assert.NotNull(card.Description);
    }

    // ── Invalid option values produce ArgumentException ──────────────────

    [Fact]
    public async Task Generate_WithInvalidRollMethod_ThrowsArgumentException()
    {
        var (module, _) = await CreateModulePipelineAsync();
        var options = new Dictionary<string, object?> { ["roll-method"] = "bananas" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenerateMulti_WithInvalidCount_ThrowsArgumentException()
    {
        var (module, _) = await CreateModulePipelineAsync();
        var options = new Dictionary<string, object?> { ["count"] = "nope" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken));
    }
}
