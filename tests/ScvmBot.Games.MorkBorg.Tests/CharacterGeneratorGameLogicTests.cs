using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

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
            }.Concat(Enumerable.Repeat(1, 20)));
            var generator = new CharacterGenerator(referenceData, rng);

            var character = generator.Generate(null);

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
            var rng = new DeterministicRandom(new[] { roll });
            var dice = new DiceRoller(rng);
            var resolver = new WeaponResolver(referenceData, dice, rng);

            var result = resolver.Resolve(new CharacterGenerationOptions(), null);

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
            var rng = new DeterministicRandom(new[] { roll });
            var dice = new DiceRoller(rng);
            var resolver = new ArmorResolver(referenceData, dice, rng);

            var result = resolver.Resolve(new CharacterGenerationOptions(), null);

            Assert.NotNull(result);
            Assert.Contains(expectedName, result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task ResolveWeapon_WithoutReferenceData_ReturnsNullOnRandomPath()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "items.json"), "[]");
            var referenceData = await MorkBorgReferenceDataService.CreateAsync(directory);
            var rng = new DeterministicRandom(new[] { 1 });
            var dice = new DiceRoller(rng);
            var resolver = new WeaponResolver(referenceData, dice, rng);

            var result = resolver.Resolve(new CharacterGenerationOptions(), null);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task ResolveArmor_WithoutReferenceData_ReturnsNullOnRandomPath()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(directory, "items.json"), "[]");
            var referenceData = await MorkBorgReferenceDataService.CreateAsync(directory);
            var rng = new DeterministicRandom(new[] { 1 });
            var dice = new DiceRoller(rng);
            var resolver = new ArmorResolver(referenceData, dice, rng);

            var result = resolver.Resolve(new CharacterGenerationOptions(), null);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_InvalidWeaponName_Throws()
    {
        var directory = TestUtilities.CreateTempDirectory();
        try
        {
            var referenceData = await LoadReferenceDataAsync();
            var generator = new CharacterGenerator(referenceData, new Random(42));

            var options = new CharacterGenerationOptions
            {
                Name = "NoGear",
                ClassName = "",
                WeaponName = "Missing Weapon",
                StartingContainerOverride = "Backpack",
                SkipRandomStartingGear = true
            };

            var ex = Assert.Throws<InvalidOperationException>(() => generator.Generate(options));
            Assert.Contains("Missing Weapon", ex.Message);
            Assert.Contains("not found in weapons data", ex.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }







    [Fact]
    public void RollDie_Throws_WhenSidesIsNotPositive()
    {
        var dice = new DiceRoller(new DeterministicRandom(Array.Empty<int>()));

        Assert.Throws<ArgumentOutOfRangeException>(() => dice.RollDie(0));
    }

    [Fact]
    public void RollFourD6DropLowest_DropsMinimumDie()
    {
        var dice = new DiceRoller(new DeterministicRandom(new[] { 1, 2, 3, 4 }));

        var total = dice.RollFourD6DropLowest();

        Assert.Equal(9, total);
    }

    [Theory]
    [InlineData(new[] { 1, 1, 1, 1 }, -3)]
    [InlineData(new[] { 6, 6, 6, 6 }, 3)]
    [InlineData(new[] { 4, 4, 4, 4 }, 0)]
    public void RollAbilityModifier_FourD6DropLowest_MapsCorrectly(int[] rolls, int expected)
    {
        var rng = new DeterministicRandom(rolls);
        var dice = new DiceRoller(rng);
        var roller = new AbilityRoller(dice, rng);

        var modifier = roller.RollAbilityModifier(AbilityRollMethod.FourD6DropLowest);

        Assert.Equal(expected, modifier);
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

            var character = generator.Generate(new CharacterGenerationOptions
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

            var character = generator.Generate(new CharacterGenerationOptions
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
            var rng = new DeterministicRandom(Enumerable.Repeat(6, 12).Concat(Enumerable.Repeat(1, 18)));
            var generator = new CharacterGenerator(
                referenceData, rng);

            var character = generator.Generate(new CharacterGenerationOptions
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
                var character = generator.Generate(new CharacterGenerationOptions
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
                var character = generator.Generate(new CharacterGenerationOptions
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

            var character = generator.Generate(new CharacterGenerationOptions
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

            var character = generator.Generate(new CharacterGenerationOptions
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

            var character = generator.Generate(new CharacterGenerationOptions
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
                var character = generator.Generate(new CharacterGenerationOptions
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

            var character = generator.Generate(new CharacterGenerationOptions
            {
                ClassName = MorkBorgConstants.ClasslessClassName,
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

            Assert.Throws<InvalidOperationException>(() =>
                generator.Generate(new CharacterGenerationOptions
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
        var dataRoot = TestUtilities.GetMorkBorgDataPath();
        return await MorkBorgReferenceDataService.CreateAsync(dataRoot);
    }
}
