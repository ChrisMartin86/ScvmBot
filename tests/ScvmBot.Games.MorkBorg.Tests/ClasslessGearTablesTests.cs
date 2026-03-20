using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Tests;

public class ClasslessGearTablesTests : MorkBorgGameRulesFixture
{
    [Fact]
    public async Task GearTableItems_AreDefinedInReferenceData()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Verify key Table A items exist
        var tableAItemsToCheck = new[] { "Rope", "Torch", "Oil lamp", "Magnesium strip", "Medicine chest", "Metal file", "Bear trap", "Bomb", "Red poison", "Life elixir", "Heavy chain", "Grappling hook" };
        foreach (var itemName in tableAItemsToCheck)
        {
            var item = refData.GetItemByName(itemName);
            Assert.NotNull(item);
            Assert.Equal(itemName, item.Name);
        }

        // Verify key Table B items exist (note: monkeys are only in descriptions, not actual items)
        var tableBItemsToCheck = new[] { "Small vicious dog", "Life elixir", "Exquisite perfume", "Lard" };
        foreach (var itemName in tableBItemsToCheck)
        {
            var item = refData.GetItemByName(itemName);
            Assert.NotNull(item);
        }
    }

    [Fact]
    public async Task Classless_GeneratesVariedEquipment_AcrossMultipleRuns()
    {
        var refData = await LoadGameReferenceDataAsync();
        var gearCollections = new List<string>();

        // Generate characters with different seeds
        for (int seed = 1; seed <= 10; seed++)
        {
            var rng = new Random(seed);
            var generator = new CharacterGenerator(refData, rng);

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "none",
            });

            // Collect and normalize gear for comparison
            var gearString = string.Join("|", character.Items.OrderBy(x => x));
            gearCollections.Add(gearString);
        }

        // Should see at least some variation across 10 runs
        var distinctCollections = gearCollections.Distinct().Count();
        Assert.True(distinctCollections > 1, $"Expected varied equipment, but got {distinctCollections} distinct gear sets across 10 runs");
    }

    [Fact]
    public async Task ScrollGeneration_DoesNotContainPlaceholders()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Generate multiple characters and look for scroll descriptions
        for (int seed = 1; seed <= 5; seed++)
        {
            var rng = new Random(seed);
            var generator = new CharacterGenerator(refData, rng);

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "none",
            });

            // Verify no descriptions contain "placeholder"
            foreach (var desc in character.Descriptions)
            {
                Assert.DoesNotContain("placeholder", desc.Text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task ClasslessWithSameSeed_ProduceIdenticalEquipment()
    {
        var refData = await LoadGameReferenceDataAsync();
        const int seed = 42;

        // Generate twice with same seed
        var char1Items = new List<string>();
        var char2Items = new List<string>();

        var rng1 = new Random(seed);
        var gen1 = new CharacterGenerator(refData, rng1);
        var char1 = await gen1.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "none",
        });
        char1Items.AddRange(char1.Items.OrderBy(x => x));

        var rng2 = new Random(seed);
        var gen2 = new CharacterGenerator(refData, rng2);
        var char2 = await gen2.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "none",
        });
        char2Items.AddRange(char2.Items.OrderBy(x => x));

        // Items should match exactly
        Assert.Equal(char1Items, char2Items);
    }

    [Fact]
    public async Task Classless_AlwaysHasBasicStartingGear()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Test multiple seeds
        for (int seed = 1; seed <= 5; seed++)
        {
            var rng = new Random(seed);
            var generator = new CharacterGenerator(refData, rng);

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "none",
            });

            // Classless should always have these basic items
            Assert.Contains(character.Items, i => i.Contains("Waterskin"));
            Assert.Contains(character.Items, i => i.Contains("Dried food"));
        }
    }

    [Fact]
    public async Task ClasslessGear_ShowsDiversity_AcrossSample()
    {
        var refData = await LoadGameReferenceDataAsync();
        var allGearItems = new HashSet<string>();

        // Generate 20 characters
        for (int seed = 1; seed <= 20; seed++)
        {
            var rng = new Random(seed);
            var generator = new CharacterGenerator(refData, rng);

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "none",
            });

            // Collect all non-basic items (exclude waterskin and food)
            var advancedGear = character.Items
                .Where(i => !i.Contains("Waterskin") && !i.Contains("Dried food"))
                .ToList();

            foreach (var gear in advancedGear)
            {
                allGearItems.Add(gear);
            }
        }

        // Across 20 characters, we should see at least 10 different gear items
        // (the d12 tables have 12 items each, so good diversity expected)
        Assert.True(allGearItems.Count >= 10,
            $"Expected at least 10 distinct gear items across sample, got {allGearItems.Count}. Items: {string.Join(", ", allGearItems.OrderBy(x => x))}");
    }

    [Fact]
    public async Task ClasslessCharacter_CanGenerateScrollFromGearTables()
    {
        var refData = await LoadGameReferenceDataAsync();
        bool foundScrollInGear = false;

        // Generate multiple classless characters with different seeds
        // Eventually one should hit the sacred scroll outcome (Table B roll 11)
        for (int seed = 1; seed <= 30 && !foundScrollInGear; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = await gen.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "none",
            });

            // If any character has scrolls, they should come from the structured path
            if (ch.ScrollsKnown.Any())
            {
                foundScrollInGear = true;
                // Verify they're real scroll entries, not placeholders
                foreach (var scroll in ch.ScrollsKnown)
                {
                    Assert.NotEmpty(scroll);
                    Assert.DoesNotContain("placeholder", scroll, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        Assert.True(foundScrollInGear, "Expected at least one classless character across 30 seeds to generate a scroll from gear tables");
    }

    [Fact]
    public async Task GearTableScrollReward_IsNotDescriptionOnly()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Multiple runs with scroll-generating positions
        for (int seed = 100; seed < 110; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = await gen.GenerateAsync(new CharacterGenerationOptions
            {
                Name = "Test",
                ClassName = "none",
            });

            // If we have scrolls, verify they're real entries
            if (ch.ScrollsKnown.Any())
            {
                // ScrollsKnown should contain formatted scroll strings
                var hasValidScroll = ch.ScrollsKnown.Any(s => !string.IsNullOrWhiteSpace(s) && s.Length > 3);
                Assert.True(hasValidScroll, $"Character has scrolls but none appear to be properly formatted. Scrolls: {string.Join("; ", ch.ScrollsKnown)}");
            }
        }
    }
}

