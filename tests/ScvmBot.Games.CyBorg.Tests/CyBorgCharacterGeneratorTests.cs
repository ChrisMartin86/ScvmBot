using ScvmBot.Games.CyBorg.Generation;
using ScvmBot.Games.CyBorg.Models;
using ScvmBot.Games.CyBorg.Reference;

namespace ScvmBot.Games.CyBorg.Tests;

public class CyBorgCharacterGeneratorTests
{
    private static string GetCyBorgDataPath() =>
        Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.CyBorg", "Data");

    private static async Task<CyBorgReferenceDataService> LoadReferenceDataAsync() =>
        await CyBorgReferenceDataService.CreateAsync(GetCyBorgDataPath());

    [Fact]
    public async Task Generate_WithNullOptions_ProducesValidCharacter()
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));

        var character = generator.Generate(null);

        Assert.False(string.IsNullOrWhiteSpace(character.Name));
        Assert.True(character.HitPoints >= 1);
        Assert.True(character.MaxHitPoints >= 1);
        Assert.True(character.Luck >= 1);
        Assert.True(character.Credits >= 0);
        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }

    [Fact]
    public async Task Generate_AllStatsInValidRange_AcrossMultipleGenerations()
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(77));

        for (var i = 0; i < 20; i++)
        {
            var character = generator.Generate(new CyBorgCharacterGenerationOptions());

            Assert.InRange(character.Strength, -3, 3);
            Assert.InRange(character.Agility, -3, 3);
            Assert.InRange(character.Presence, -3, 3);
            Assert.InRange(character.Toughness, -3, 3);
            Assert.True(character.HitPoints >= 1);
            Assert.True(character.MaxHitPoints >= 1);
            Assert.True(character.Luck >= 1);
            Assert.True(character.Credits >= 0);
        }
    }

    [Theory]
    [InlineData("Nano-witch")]
    [InlineData("Street punk")]
    [InlineData("Wetware hacker")]
    [InlineData("Burned-out data courier")]
    [InlineData("Class-A android")]
    [InlineData("Drifter")]
    public async Task Generate_AllOfficialClasses_ProduceValidCharacters(string className)
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));

        var character = generator.Generate(new CyBorgCharacterGenerationOptions
        {
            ClassName = className
        });

        Assert.Equal(className, character.ClassName);
        Assert.NotNull(character.ClassAbility);
        Assert.NotEmpty(character.ClassAbility);
        Assert.True(character.HitPoints >= 1);
        Assert.True(character.Luck >= 1);
        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }

    [Fact]
    public async Task Generate_ClasslessCharacter_HasNoClassName()
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));

        var character = generator.Generate(new CyBorgCharacterGenerationOptions
        {
            ClassName = "none"
        });

        Assert.Null(character.ClassName);
        Assert.Null(character.ClassAbility);
    }

    [Fact]
    public async Task Generate_InvalidClassName_Throws()
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            generator.Generate(new CyBorgCharacterGenerationOptions
            {
                ClassName = "Totally Fake Class"
            }));

        Assert.Contains("Totally Fake Class", ex.Message);
    }

    [Fact]
    public async Task Generate_NanoWitch_HasAtLeastOneApp()
    {
        var refData = await LoadReferenceDataAsync();

        for (var seed = 1; seed <= 5; seed++)
        {
            var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(seed));
            var character = generator.Generate(new CyBorgCharacterGenerationOptions
            {
                ClassName = "Nano-witch"
            });

            Assert.NotEmpty(character.Apps);
        }
    }

    [Fact]
    public async Task Generate_WetwareHacker_HasAtLeastTwoApps()
    {
        var refData = await LoadReferenceDataAsync();

        for (var seed = 1; seed <= 5; seed++)
        {
            var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(seed));
            var character = generator.Generate(new CyBorgCharacterGenerationOptions
            {
                ClassName = "Wetware hacker"
            });

            Assert.True(character.Apps.Count >= 2, $"Wetware hacker should have at least 2 apps, got {character.Apps.Count}");
        }
    }

    [Fact]
    public async Task Generate_WithNameOverride_UsesSuppliedName()
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));

        var character = generator.Generate(new CyBorgCharacterGenerationOptions
        {
            Name = "TestHandle"
        });

        Assert.Equal("TestHandle", character.Name);
    }

    [Fact]
    public async Task Generate_ClassAbility_DisplayedForClasses()
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(99));

        var character = generator.Generate(new CyBorgCharacterGenerationOptions
        {
            ClassName = "Street punk"
        });

        Assert.NotNull(character.ClassAbility);
        Assert.NotEmpty(character.ClassAbility);
        Assert.Contains("Brawler", character.ClassAbility, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Generate_AlwaysHasThreeDescriptions()
    {
        var refData = await LoadReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));

        var character = generator.Generate(null);

        Assert.Equal(3, character.Descriptions.Count);
        Assert.Contains(character.Descriptions, d => d.Category == CyBorgDescriptionCategory.Trait);
        Assert.Contains(character.Descriptions, d => d.Category == CyBorgDescriptionCategory.Appearance);
        Assert.Contains(character.Descriptions, d => d.Category == CyBorgDescriptionCategory.Glitch);
    }

    [Theory]
    [InlineData("d6", 6)]
    [InlineData("D8", 8)]
    [InlineData("d4", 4)]
    [InlineData("d10", 10)]
    [InlineData("", 6)]
    [InlineData(null, 6)]
    public void ParseDieSize_ParsesCorrectly(string? die, int expected)
    {
        Assert.Equal(expected, CyBorgCharacterGenerator.ParseDieSize(die!));
    }

    [Fact]
    public async Task ReferenceDataService_LoadsAllRequiredFiles()
    {
        var refData = await LoadReferenceDataAsync();

        Assert.NotEmpty(refData.Names);
        Assert.NotEmpty(refData.Classes);
        Assert.NotEmpty(refData.Weapons);
        Assert.NotEmpty(refData.Armor);
        Assert.NotEmpty(refData.Gear);
    }

    [Fact]
    public async Task ReferenceDataService_MissingRequiredFile_Throws()
    {
        var tempDir = SharedTestInfrastructure.CreateTempDirectory();
        try
        {
            // Write only some files, omit 'names.json' which is required
            await File.WriteAllTextAsync(Path.Combine(tempDir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "gear.json"), "[]", TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                CyBorgReferenceDataService.CreateAsync(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AbilityRoller_ModifierTable_IsCorrect()
    {
        var dice = new CyBorgDiceRoller(new DeterministicRandom(new[] { 1, 1, 1 }));
        var roller = new CyBorgAbilityRoller(dice);

        var modifier = roller.RollAbilityModifier();

        Assert.Equal(-3, modifier);
    }

    [Theory]
    [InlineData(new[] { 1, 1, 1 }, -3)]
    [InlineData(new[] { 2, 2, 2 }, -2)]
    [InlineData(new[] { 3, 3, 2 }, -1)]
    [InlineData(new[] { 4, 4, 4 }, 0)]
    [InlineData(new[] { 5, 5, 4 }, 1)]
    [InlineData(new[] { 6, 5, 5 }, 2)]
    [InlineData(new[] { 6, 6, 6 }, 3)]
    public void AbilityRoller_ModifierTable_MapsCorrectly(int[] rolls, int expected)
    {
        var dice = new CyBorgDiceRoller(new DeterministicRandom(rolls));
        var roller = new CyBorgAbilityRoller(dice);

        var modifier = roller.RollAbilityModifier();

        Assert.Equal(expected, modifier);
    }

    [Fact]
    public void DiceRoller_RollDie_Throws_WhenSidesNotPositive()
    {
        var dice = new CyBorgDiceRoller(new DeterministicRandom(Array.Empty<int>()));

        Assert.Throws<ArgumentOutOfRangeException>(() => dice.RollDie(0));
    }
}
