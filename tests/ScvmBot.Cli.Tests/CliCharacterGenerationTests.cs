using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Cli.Tests;

/// <summary>
/// Proves the CLI host path: game logic is consumable without Discord.Net
/// and produces the same results as the bot path.
/// </summary>
public class CliCharacterGenerationTests
{
    private static async Task<(MorkBorgReferenceDataService RefData, CharacterGenerator Generator)> CreateGeneratorAsync()
    {
        var dataPath = Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "games", "ScvmBot.Games.MorkBorg", "Data");
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
        var dataPath = Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "games", "ScvmBot.Games.MorkBorg", "Data");
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
}
