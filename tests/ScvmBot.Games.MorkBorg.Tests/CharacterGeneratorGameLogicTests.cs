using ScvmBot.Bot.Games.MorkBorg;
using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Games.MorkBorg.Tests;

public class CharacterGeneratorGameLogicTests
{
    [Fact]
    public async Task GenerateAsync_WithNullOptions_UsesDefaultRandomPath()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var rng = new DeterministicRandom(new[]
            {
                2, 2, 2, 2,
                3,
                1,
                2,
                1,
                2,
                1,
                1,
                1,
                1
            });
            var generator = new CharacterGenerator(referenceData, rng);

            var character = await generator.GenerateAsync(null);

            Assert.False(string.IsNullOrWhiteSpace(character.Name));
            Assert.NotNull(character.EquippedWeapon);
            Assert.NotNull(character.EquippedArmor);
            Assert.True(character.HitPoints >= 1);
            Assert.True(character.Omens is >= 1 and <= 2);
            Assert.True(character.Silver >= 20);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(1, "Femur")]
    [InlineData(2, "Staff")]
    [InlineData(3, "Shortsword")]
    [InlineData(4, "Knife")]
    [InlineData(5, "Warhammer")]
    [InlineData(6, "Sword")]
    [InlineData(7, "Bow")]
    [InlineData(8, "Flail")]
    [InlineData(9, "Crossbow")]
    [InlineData(10, "Zweihänder")]
    public async Task ResolveWeapon_RandomTable_ReturnsMappedWeapon(int roll, string expectedName)
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(referenceData, new DeterministicRandom(new[] { roll }));

            var result = TestUtilities.InvokePrivate<string?>(generator, "ResolveWeapon", new CharacterGenerationOptions(), null!);

            Assert.NotNull(result);
            Assert.Contains(expectedName, result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(1, "No armor")]
    [InlineData(2, "Light armor")]
    [InlineData(3, "Medium armor")]
    [InlineData(4, "Heavy armor")]
    public async Task ResolveArmor_RandomTable_ReturnsMappedArmor(int roll, string expectedName)
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(referenceData, new DeterministicRandom(new[] { roll }));

            var result = TestUtilities.InvokePrivate<string?>(generator, "ResolveArmor", new CharacterGenerationOptions(), null!);

            Assert.NotNull(result);
            Assert.Contains(expectedName, result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ResolveWeapon_WithoutReferenceData_ReturnsNullOnRandomPath()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = new MorkBorgReferenceDataService();
            var generator = new CharacterGenerator(referenceData, new DeterministicRandom(new[] { 1 }));

            var result = TestUtilities.InvokePrivate<string?>(generator, "ResolveWeapon", new CharacterGenerationOptions(), null!);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ResolveArmor_WithoutReferenceData_ReturnsNullOnRandomPath()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = new MorkBorgReferenceDataService();
            var generator = new CharacterGenerator(referenceData, new DeterministicRandom(new[] { 1 }));

            var result = TestUtilities.InvokePrivate<string?>(generator, "ResolveArmor", new CharacterGenerationOptions(), null!);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_InvalidWeaponAndArmorNames_ResultInNullEquipment()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var rng = new DeterministicRandom(new[] { 2, 0, 0, 0, 0, 1, 1, 1 });
            var generator = new CharacterGenerator(referenceData, rng);

            var options = new CharacterGenerationOptions
            {
                Name = "NoGear",
                ClassName = "",
                Strength = 0,
                Agility = 0,
                Presence = 0,
                Toughness = 0,
                Omens = 1,
                HitPoints = 5,
                MaxHitPoints = 5,
                Silver = 20,
                WeaponName = "Missing Weapon",
                ArmorName = "Missing Armor",
                StartingContainerOverride = "Backpack",
                SkipRandomStartingGear = true
            };

            var character = await generator.GenerateAsync(options);

            Assert.Null(character.EquippedWeapon);
            Assert.Null(character.EquippedArmor);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }







    [Fact]
    public async Task RollDie_Throws_WhenSidesIsNotPositive()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(referenceData, new DeterministicRandom(Array.Empty<int>()));

            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                TestUtilities.InvokePrivate<int>(generator, "RollDie", 0));

            Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RollFourD6DropLowest_DropsMinimumDie()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData,
                new DeterministicRandom(new[] { 1, 2, 3, 4 }));

            var total = TestUtilities.InvokePrivate<int>(generator, "RollFourD6DropLowest");

            Assert.Equal(9, total);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(new[] { 1, 1, 1, 1 }, -3)]
    [InlineData(new[] { 6, 6, 6, 6 }, 3)]
    [InlineData(new[] { 4, 4, 4, 4 }, 0)]
    public async Task RollAbilityModifier_FourD6DropLowest_MapsCorrectly(int[] rolls, int expected)
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData,
                new DeterministicRandom(rolls));

            var modifier = TestUtilities.InvokePrivate<int>(
                generator, "RollAbilityModifier", AbilityRollMethod.FourD6DropLowest);

            Assert.Equal(expected, modifier);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithFourD6DropLowest_ProducesValidCharacter()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData, new Random(42));

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                RollMethod = AbilityRollMethod.FourD6DropLowest,
            });

            Assert.False(string.IsNullOrWhiteSpace(character.Name));
            Assert.True(character.HitPoints >= 1);
            Assert.InRange(character.Strength, -3, 3);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData("Esoteric Hermit")]
    [InlineData("Fanged Deserter")]
    [InlineData("Gutterborn Scum")]
    [InlineData("Heretical Priest")]
    [InlineData("Occult Herbmaster")]
    [InlineData("Wretched Royalty")]
    public async Task GenerateAsync_AllOfficialClasses_ProduceValidCharacters(string className)
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData, new Random(42));

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = className,
            });

            Assert.Equal(className, character.ClassName);
            Assert.NotNull(character.ClassAbility);
            Assert.True(character.HitPoints >= 1);
            Assert.True(character.Omens >= 1);
            Assert.InRange(character.Strength, -3, 3);
            Assert.InRange(character.Agility, -3, 3);
            Assert.InRange(character.Presence, -3, 3);
            Assert.InRange(character.Toughness, -3, 3);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }



    [Fact]
    public async Task GenerateAsync_ClassedCharacter_IgnoresFourD6DropLowest()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var rng = new DeterministicRandom(new[] { 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6 });
            var generator = new CharacterGenerator(
                referenceData, rng);

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "Fanged Deserter",
                RollMethod = AbilityRollMethod.FourD6DropLowest,
            });

            Assert.Equal("Fanged Deserter", character.ClassName);
            Assert.InRange(character.Strength, -3, 3);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }





    [Fact]
    public async Task GenerateAsync_AllStatsInValidRange()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData, new Random(77));

            for (int i = 0; i < 20; i++)
            {
                var character = await generator.GenerateAsync(new CharacterGenerationOptions
                {
                });

                Assert.InRange(character.Strength, -3, 3);
                Assert.InRange(character.Agility, -3, 3);
                Assert.InRange(character.Presence, -3, 3);
                Assert.InRange(character.Toughness, -3, 3);

                Assert.True(character.Omens >= 1);
                Assert.True(character.HitPoints >= 1);
                Assert.True(character.MaxHitPoints >= 1);

                Assert.True(character.Silver >= 0);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_ScrollsAreValidOfficialScrolls()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var allOfficialScrolls = referenceData.Scrolls;

            var generator = new CharacterGenerator(
                referenceData, new Random(88));

            for (int i = 0; i < 10; i++)
            {
                var character = await generator.GenerateAsync(new CharacterGenerationOptions
                {
                });

                foreach (var scrollStr in character.ScrollsKnown)
                {
                    var matchFound = allOfficialScrolls.Any(s => scrollStr.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
                    Assert.True(matchFound, $"Scroll '{scrollStr}' not found in official list");
                }
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_ClassAbilityDisplayedInOutput()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData, new Random(99));

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "Fanged Deserter",
            });

            Assert.NotNull(character.ClassAbility);
            Assert.NotEmpty(character.ClassAbility);
            Assert.Contains("Bite", character.ClassAbility, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_HereticalPriestIncludesSacredScroll()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData, new Random(111));

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "Heretical Priest",
            });

            Assert.Equal("Heretical Priest", character.ClassName);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_EsotericHermitIncludesUncleanScroll()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(
                referenceData, new Random(222));

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "Esoteric Hermit",
            });

            Assert.Equal("Esoteric Hermit", character.ClassName);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    // =====================================================================
    // ParseDieSize edge cases
    // =====================================================================

    [Theory]
    [InlineData("d8", 8)]
    [InlineData("D10", 10)]
    [InlineData("d4", 4)]
    [InlineData("d2", 2)]
    [InlineData("d100", 100)]
    public void ParseDieSize_ParsesValidDieStrings(string die, int expected)
    {
        Assert.Equal(expected, CharacterGenerator.ParseDieSize(die));
    }

    [Theory]
    [InlineData("", 8)]
    [InlineData("   ", 8)]
    [InlineData(null, 8)]
    [InlineData("abc", 8)]
    [InlineData("d0", 8)]
    [InlineData("d-1", 8)]
    public void ParseDieSize_ReturnsFallback_ForInvalidInput(string? die, int expected)
    {
        Assert.Equal(expected, CharacterGenerator.ParseDieSize(die!));
    }

    // =====================================================================
    // GetRandomAnyScroll via Esoteric Hermit
    // =====================================================================

    [Fact]
    public async Task EsotericHermit_AlwaysGetsARandomScroll()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var refData = await LoadReferenceDataAsync();

            for (int seed = 1; seed <= 10; seed++)
            {
                var generator = new CharacterGenerator(refData, new Random(seed));
                var character = await generator.GenerateAsync(new CharacterGenerationOptions
                {
                    ClassName = "Esoteric Hermit",
                });

                // Esoteric Hermit's startingScrolls contains "random_any_scroll"
                Assert.NotEmpty(character.ScrollsKnown);
                Assert.All(character.ScrollsKnown, s => Assert.False(string.IsNullOrWhiteSpace(s)));
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    // =====================================================================
    // ForceItemNames
    // =====================================================================

    [Fact]
    public async Task ForceItemNames_AddsExtraItems()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var refData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(refData, new Random(42));

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "none",
                ForceItemNames = { "Rope", "Torch" },
            });

            Assert.Contains(character.Items, i => i.Contains("Rope"));
            Assert.Contains(character.Items, i => i.Contains("Torch"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    // =====================================================================
    // Unknown class name
    // =====================================================================

    [Fact]
    public async Task GenerateAsync_Throws_WhenClassNameIsInvalid()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var refData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(refData, new Random(42));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                generator.GenerateAsync(new CharacterGenerationOptions
                {
                    ClassName = "Totally Fake Class",
                }));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static async Task<MorkBorgReferenceDataService> LoadReferenceDataAsync()
    {
        var dataRoot = Path.Combine(TestUtilities.GetBotProjectPath(), "Data", "MorkBorg");
        var service = new MorkBorgReferenceDataService(dataRoot);
        await service.LoadDataAsync();
        return service;
    }
}
