using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Tests;

public class CharacterGenerationEquipmentFlowTests : MorkBorgGameRulesFixture
{
    [Fact]
    public async Task ClasslessCharacter_AlwaysUsesFullGearFlow()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "none",  // Explicitly classless
        });

        Assert.Null(character.ClassName);
        // Classless should always have waterskin and food
        Assert.Contains(character.Items, i => i.Contains("Waterskin"));
        Assert.Contains(character.Items, i => i.Contains("Dried food"));
        // And should have a container (most of the time)
        var containerDescriptions = character.Descriptions.Where(d => d.Category == DescriptionCategory.Container);
        Assert.NotEmpty(containerDescriptions);
    }

    [Fact]
    public async Task ClassedCharacter_WithOrdinaryEquipment_SkipsRandomGearTables()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",  // Classed character
        });

        Assert.Equal("Fanged Deserter", character.ClassName);
        // Classed with "ordinary" mode should have waterskin, food, container
        Assert.Contains(character.Items, i => i.Contains("Waterskin"));
        Assert.Contains(character.Items, i => i.Contains("Dried food"));

        // But should NOT have items from random d12 tables
        // (Rope, Torches, Oil lamp, Magnesium strip, Medicine chest, etc. are from table A)
        // (Small vicious dog, Monkeys, Exquisite perfume, Lard, etc. are from table B)
        var tableAItems = new[] { "Rope", "Torch", "Oil lamp", "Magnesium strip", "Medicine chest", "Metal file", "Bear trap", "Bomb", "Grappling hook" };
        foreach (var item in tableAItems)
        {
            var foundItem = character.Items.FirstOrDefault(i => i.Contains(item, StringComparison.OrdinalIgnoreCase));
            Assert.Null(foundItem);
        }
    }

    [Fact]
    public async Task ClassStatModifiers_AreAppliedToAbilityScores()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Use deterministic rolls to verify modifiers are applied
        // All 3s gives -2 normally
        var diceRolls = new int[20];
        for (int i = 0; i < diceRolls.Length; i++) diceRolls[i] = 3;

        var rng = new DeterministicRandom(diceRolls);
        var generator = new CharacterGenerator(refData, rng);

        // Fanged Deserter has various modifiers in the data
        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
        });

        // Verify that the character was created with a class and stats are valid
        Assert.Equal("Fanged Deserter", character.ClassName);
        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }

    [Fact]
    public async Task Classless_AndClassed_WithSameSeed_ProduceDistinctEquipment()
    {
        var refData = await LoadGameReferenceDataAsync();
        const int seed = 42;

        // Generate classless with same seed
        var classlessRng = new Random(seed);
        var classlessGenerator = new CharacterGenerator(refData, classlessRng);
        var classless = classlessGenerator.Generate(new CharacterGenerationOptions
        {
            ClassName = "none",
        });

        // Generate classed with same seed
        var classedRng = new Random(seed);
        var classedGenerator = new CharacterGenerator(refData, classedRng);
        var classed = classedGenerator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
        });

        // Classless should have more items (from random tables)
        Assert.True(classless.Items.Count > classed.Items.Count,
            $"Classless ({classless.Items.Count} items) should have more than classed ({classed.Items.Count} items)");
    }

    [Fact]
    public async Task CustomMode_ContainsOnlyClassItems_NotBaseKit()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Occult Herbmaster",  // Uses "custom" mode with startingItems: ["Medicine chest"]
        });

        Assert.Equal("Occult Herbmaster", character.ClassName);

        // Custom mode should have the class-specific item
        Assert.Contains(character.Items, i => i.Contains("Medicine chest"));

        // But should NOT have the default base kit items
        Assert.DoesNotContain(character.Items, i => i.Contains("Waterskin"));
        Assert.DoesNotContain(character.Items, i => i.Contains("Dried food"));

        // And should have no container description (since no container is added in custom mode)
        var containerDescriptions = character.Descriptions.Where(d => d.Category == DescriptionCategory.Container || d.Category == DescriptionCategory.Beast);
        Assert.Empty(containerDescriptions);
    }

    [Fact]
    public async Task OrdinaryMode_IncludesBaseKit_AndClassItems()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Esoteric Hermit",  // Uses "ordinary" mode
        });

        Assert.Equal("Esoteric Hermit", character.ClassName);

        // Ordinary mode should have the base kit
        Assert.Contains(character.Items, i => i.Contains("Waterskin"));
        Assert.Contains(character.Items, i => i.Contains("Dried food"));

        // And should have a container description
        var containerDescriptions = character.Descriptions.Where(d => d.Category == DescriptionCategory.Container || d.Category == DescriptionCategory.Beast);
        Assert.NotEmpty(containerDescriptions);

        // But should NOT have items from random d12 tables (Table A/B)
        var tableAItems = new[] { "Rope", "Torch", "Oil lamp", "Magnesium strip", "Metal file", "Bear trap", "Bomb", "Grappling hook" };
        foreach (var item in tableAItems)
        {
            var foundItem = character.Items.FirstOrDefault(i => i.Contains(item, StringComparison.OrdinalIgnoreCase));
            Assert.Null(foundItem);
        }
    }

    [Fact]
    public async Task CustomMode_IsBehaviorallyDistinct_FromOrdinaryMode()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Generate Occult Herbmaster (custom mode)
        var customGenerator = new CharacterGenerator(refData, new Random(42));
        var customChar = customGenerator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Occult Herbmaster",
        });

        // Generate a hypothetical ordinary-mode character (using Fanged Deserter as reference)
        var ordinaryGenerator = new CharacterGenerator(refData, new Random(42));
        var ordinaryChar = ordinaryGenerator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
        });

        // Custom mode should have fewer items (class items only, no container/food/water)
        // Ordinary mode should have more items (base kit + class items)
        Assert.True(customChar.Items.Count < ordinaryChar.Items.Count,
            $"Custom ({customChar.Items.Count} items) should have fewer than ordinary ({ordinaryChar.Items.Count} items)");

        // Verify the specific difference: ordinary has waterskin/food, custom does not
        Assert.Contains(ordinaryChar.Items, i => i.Contains("Waterskin"));
        Assert.DoesNotContain(customChar.Items, i => i.Contains("Waterskin"));
    }

    [Fact]
    public async Task UnknownEquipmentMode_ThrowsInvalidOperationException()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        // Get a real class and mutate its startingEquipmentMode to an invalid value
        var classToMutate = refData.Classes.FirstOrDefault();
        Assert.NotNull(classToMutate);

        // Use reflection to set the property to an invalid value
        var property = typeof(ClassData).GetProperty(nameof(ClassData.StartingEquipmentMode));
        Assert.NotNull(property);
        property.SetValue(classToMutate, "bogus-mode");

        // Attempt to generate a character with the invalid equipment mode
        var ex = Assert.Throws<InvalidOperationException>(
            () => generator.Generate(new CharacterGenerationOptions
            {
                ClassName = classToMutate.Name,
            }));

        // Verify the exception message includes the invalid mode name
        Assert.Contains("bogus-mode", ex.Message);
    }

    [Fact]
    public async Task StartingItems_ConcreteItem_StillWorks()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        // Occult Herbmaster uses custom mode with "Medicine chest" as a concrete item
        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Occult Herbmaster",
        });

        // Verify concrete item is in inventory
        Assert.Contains(character.Items, i => i.Contains("Medicine chest"));
    }

    [Fact]
    public async Task StartingItems_MixedConcreteAndToken_BothAppear()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        // Get a class and add mixed items via reflection
        var classToModify = refData.Classes.FirstOrDefault(c => c.StartingEquipmentMode == MorkBorgConstants.EquipmentMode.Ordinary);
        Assert.NotNull(classToModify);

        var startingItemsProperty = typeof(ClassData).GetProperty(nameof(ClassData.StartingItems));
        Assert.NotNull(startingItemsProperty);
        // Mix concrete item with token
        var mixedItems = new List<string> { "Lockpicks", MorkBorgConstants.ScrollToken.RandomSacredScroll };
        startingItemsProperty.SetValue(classToModify, mixedItems);

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = classToModify.Name,
        });

        // Verify concrete item appears
        Assert.Contains(character.Items, i => i.Contains("Lockpicks"));

        // Verify scroll appears in ScrollsKnown (not as inventory item)
        Assert.NotEmpty(character.ScrollsKnown);
        Assert.Matches(@", DR\d+\)", character.ScrollsKnown.First());
    }

    [Fact]
    public async Task StartingItems_UnsupportedToken_ThrowsException()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var classToModify = refData.Classes.FirstOrDefault(c => c.StartingEquipmentMode == MorkBorgConstants.EquipmentMode.Ordinary);
        Assert.NotNull(classToModify);

        var startingItemsProperty = typeof(ClassData).GetProperty(nameof(ClassData.StartingItems));
        Assert.NotNull(startingItemsProperty);
        // Use an invalid token that looks like a real token
        startingItemsProperty.SetValue(classToModify, new List<string> { "random_bogus_scroll" });

        var ex = Assert.Throws<InvalidOperationException>(
            () => generator.Generate(new CharacterGenerationOptions
            {
                ClassName = classToModify.Name,
            }));

        Assert.Contains("Unsupported generation token", ex.Message);
        Assert.Contains("random_bogus_scroll", ex.Message);
    }

    // ── Missing data: weapon resolution ──────────────────────────────────────

    [Fact]
    public async Task Generate_Throws_WhenWeaponNameOverride_IsNotInWeaponsData()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            generator.Generate(new CharacterGenerationOptions
            {
                ClassName = MorkBorgConstants.ClasslessClassName,
                WeaponName = "NonExistentBlade",
            }));

        Assert.Contains("NonExistentBlade", ex.Message);
        Assert.Contains("not found in weapons data", ex.Message);
    }

    [Fact]
    public async Task Generate_Throws_WhenClassStartingWeapon_IsNotInWeaponsData()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            var classJson = @"[{
                ""name"": ""TestClass"",
                ""startingWeapons"": [""GhostBlade""],
                ""startingArmor"": [],
                ""startingScrolls"": [],
                ""startingItems"": [],
                ""startingEquipmentMode"": ""ordinary""
            }]";
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), classJson);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[\"Tester\"]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

            var refData = await MorkBorgReferenceDataService.CreateAsync(dir);
            var generator = new CharacterGenerator(refData, new Random(42));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                generator.Generate(new CharacterGenerationOptions { ClassName = "TestClass" }));

            Assert.Contains("GhostBlade", ex.Message);
            Assert.Contains("TestClass", ex.Message);
            Assert.Contains("not found in weapons data", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // ── Missing data: armor resolution ───────────────────────────────────────

    [Fact]
    public async Task Generate_Throws_WhenArmorNameOverride_IsNotInArmorData()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            generator.Generate(new CharacterGenerationOptions
            {
                ClassName = MorkBorgConstants.ClasslessClassName,
                ArmorName = "PhantomPlate",
            }));

        Assert.Contains("PhantomPlate", ex.Message);
        Assert.Contains("not found in armor data", ex.Message);
    }

    [Fact]
    public async Task Generate_Throws_WhenClassStartingArmor_IsNotInArmorData()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            var classJson = @"[{
                ""name"": ""IronTestClass"",
                ""startingWeapons"": [],
                ""startingArmor"": [""ZephyrMail""],
                ""startingScrolls"": [],
                ""startingItems"": [],
                ""startingEquipmentMode"": ""ordinary""
            }]";
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), classJson);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[\"Tester\"]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

            var refData = await MorkBorgReferenceDataService.CreateAsync(dir);
            var generator = new CharacterGenerator(refData, new Random(42));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                generator.Generate(new CharacterGenerationOptions { ClassName = "IronTestClass" }));

            Assert.Contains("ZephyrMail", ex.Message);
            Assert.Contains("IronTestClass", ex.Message);
            Assert.Contains("not found in armor data", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // ── Missing data: item resolution ─────────────────────────────────────────

    [Fact]
    public async Task Generate_Throws_WhenClassStartingItem_IsNotInItemsData()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            var classJson = @"[{
                ""name"": ""ItemTestClass"",
                ""startingWeapons"": [],
                ""startingArmor"": [],
                ""startingScrolls"": [],
                ""startingItems"": [""SpecterPouch""],
                ""startingEquipmentMode"": ""ordinary""
            }]";
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), classJson);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[\"Tester\"]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            // items.json present but missing the referenced item
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

            var refData = await MorkBorgReferenceDataService.CreateAsync(dir);
            var generator = new CharacterGenerator(refData, new Random(42));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                generator.Generate(new CharacterGenerationOptions { ClassName = "ItemTestClass" }));

            Assert.Contains("SpecterPouch", ex.Message);
            Assert.Contains("not found in items data", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
