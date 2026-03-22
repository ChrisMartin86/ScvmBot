using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Tests;

public class MorkBorgComprehensiveGameTests : MorkBorgGameRulesFixture
{
    [Theory]
    [InlineData("")]
    [InlineData("Esoteric Hermit")]
    [InlineData("Fanged Deserter")]
    [InlineData("Gutterborn Scum")]
    [InlineData("Heretical Priest")]
    [InlineData("Occult Herbmaster")]
    [InlineData("Wretched Royalty")]
    public async Task Character_HasAllRequiredFields(string className)
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(444);
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = className.Length > 0 ? className : null,
        });

        // All characters must have
        Assert.False(string.IsNullOrWhiteSpace(character.Name));
        Assert.True(character.HitPoints >= 1);
        Assert.True(character.MaxHitPoints >= 1);
        Assert.True(character.Omens >= 1);
        Assert.True(character.Silver >= 0);

        // If classed, must have class info
        if (className.Length > 0)
        {
            Assert.Equal(className, character.ClassName);
            Assert.NotNull(character.ClassAbility);
        }
        else
        {
            Assert.Null(character.ClassName);
        }
    }

    [Fact]
    public async Task Character_Generation_IsConsistent_WithSameSeed()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        const int seed = 555;
        Character char1, char2;

        {
            var rng = new Random(seed);
            var generator = new CharacterGenerator(referenceData, rng);
            char1 = generator.Generate(new CharacterGenerationOptions
            {
            });
        }

        {
            var rng = new Random(seed);
            var generator = new CharacterGenerator(referenceData, rng);
            char2 = generator.Generate(new CharacterGenerationOptions
            {
            });
        }

        // Same seed should produce same character stats
        Assert.Equal(char1.Name, char2.Name);
        Assert.Equal(char1.Strength, char2.Strength);
        Assert.Equal(char1.Agility, char2.Agility);
        Assert.Equal(char1.Presence, char2.Presence);
        Assert.Equal(char1.Toughness, char2.Toughness);
        Assert.Equal(char1.HitPoints, char2.HitPoints);
        Assert.Equal(char1.Omens, char2.Omens);
        Assert.Equal(char1.Silver, char2.Silver);
    }

    [Fact]
    public async Task Stress_Test_Generate1000Characters()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(666);
        var generator = new CharacterGenerator(referenceData, rng);

        for (int i = 0; i < 1000; i++)
        {
            var character = generator.Generate(new CharacterGenerationOptions
            {
            });

            // Validate each character
            Assert.True(character.HitPoints >= 1);
            Assert.True(character.Omens >= 1);
            Assert.InRange(character.Strength, -3, 3);
            Assert.InRange(character.Agility, -3, 3);
            Assert.InRange(character.Presence, -3, 3);
            Assert.InRange(character.Toughness, -3, 3);

        }
    }

    [Fact]
    public async Task AllClasses_Generate100TimesEach_WithoutErrors()
    {
        var classNames = new[]
        {
            "Esoteric Hermit",
            "Fanged Deserter",
            "Gutterborn Scum",
            "Heretical Priest",
            "Occult Herbmaster",
            "Wretched Royalty"
        };

        var referenceData = await LoadGameReferenceDataAsync();
        var baseRng = new Random(777);

        foreach (var className in classNames)
        {
            for (int i = 0; i < 100; i++)
            {
                var rng = new Random(baseRng.Next());
                var generator = new CharacterGenerator(referenceData, rng);

                var character = generator.Generate(new CharacterGenerationOptions
                {
                    ClassName = className,
                });

                Assert.Equal(className, character.ClassName);
                Assert.True(character.HitPoints >= 1);
                Assert.NotNull(character.ClassAbility);
            }
        }
    }

    [Fact]
    public async Task Character_WithAllOptionsSet_GeneratesSuccessfully()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(888);
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(new CharacterGenerationOptions
        {
            Name = "Test Character",
            ClassName = "Fanged Deserter",
            RollMethod = AbilityRollMethod.FourD6DropLowest,  // Should be ignored for classed
            Strength = 2,
            Agility = 1,
            Presence = -1,
            Toughness = 0,
            Omens = 2,
            HitPoints = 10,
            MaxHitPoints = 10,
            Silver = 50,
            WeaponName = "Sword",
            ArmorName = "Medium armor",
            StartingContainerOverride = "Backpack",
            SkipRandomStartingGear = true
        });

        // Verify overrides were applied
        Assert.Equal("Test Character", character.Name);
        Assert.Equal("Fanged Deserter", character.ClassName);
        Assert.Equal(2, character.Strength);
        Assert.Equal(1, character.Agility);
        Assert.Equal(-1, character.Presence);
        Assert.Equal(0, character.Toughness);
        Assert.Equal(2, character.Omens);
        Assert.Equal(10, character.HitPoints);
        Assert.Equal(10, character.MaxHitPoints);
        Assert.Equal(50, character.Silver);
    }

    [Fact]
    public async Task NullOptions_UsesDefaults()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(999);
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(null);

        // Should still generate valid character
        Assert.False(string.IsNullOrWhiteSpace(character.Name));
        Assert.True(character.HitPoints >= 1);
        Assert.True(character.Omens >= 1);
        Assert.InRange(character.Strength, -3, 3);
    }

    [Fact]
    public async Task RandomClass_Selection_IsRandom()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var classNames = new HashSet<string>();

        // Generate multiple characters with random class selection
        for (int i = 0; i < 50; i++)
        {
            var rng = new Random(i);
            var generator = new CharacterGenerator(referenceData, rng);

            var character = generator.Generate(new CharacterGenerationOptions
            {
                ClassName = null,  // Allow random
            });

            if (character.ClassName != null)
            {
                classNames.Add(character.ClassName);
            }
            else
            {
                classNames.Add("(classless)");
            }
        }

        // With 50 iterations, we should see multiple classes represented
        // (might not get all 6, but should get variety)
        Assert.True(classNames.Count > 1);
    }

    [Fact]
    public async Task Character_Output_IncludesAllGameInformation()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new DeterministicRandom(new[] { 3, 3, 3, 3, 2, 1, 1, 1 }.Concat(Enumerable.Repeat(1, 25)));
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Esoteric Hermit",
        });

        // Character should have complete information for output
        Assert.NotEmpty(character.Name);
        Assert.NotEmpty(character.Items);
        Assert.NotEmpty(character.Descriptions);
        Assert.True(character.ScrollsKnown.Count >= 0);  // May or may not have scrolls

        // All numeric fields should be set
        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }
}
