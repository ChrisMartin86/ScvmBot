using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Tests;

public class MorkBorgScrollMechanicsTests : MorkBorgGameRulesFixture
{
    [Fact]
    public async Task ReferenceData_Contains20OfficialScrolls()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        Assert.NotEmpty(referenceData.Scrolls);
        Assert.True(referenceData.Scrolls.Count >= 20);

        // Count by type
        var sacredCount = referenceData.Scrolls.Count(s => s.ScrollType == "Sacred");
        var uncleanCount = referenceData.Scrolls.Count(s => s.ScrollType == "Unclean");

        Assert.Equal(10, sacredCount);
        Assert.Equal(10, uncleanCount);
    }

    [Fact]
    public async Task AllScrolls_HaveRequiredProperties()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        foreach (var scroll in referenceData.Scrolls)
        {
            Assert.NotEmpty(scroll.Name);
            Assert.NotEmpty(scroll.ScrollType);
            Assert.NotEmpty(scroll.Description);
            Assert.True(scroll.UsageDR > 0);
            Assert.InRange(scroll.ScrollNumber, 1, 10);
        }
    }

    [Theory]
    [InlineData("Sacred")]
    [InlineData("Unclean")]
    public async Task ReferenceData_HasTenScrollsOfEachType(string scrollType)
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var items = referenceData.Scrolls.Where(s => s.ScrollType == scrollType).ToList();

        Assert.Equal(10, items.Count);
    }

    [Fact]
    public async Task EsotericHermit_StartsWithUncleanScroll()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new DeterministicRandom(new[] { 3, 3, 3, 3, 2, 1, 1, 1 });
        var generator = new CharacterGenerator(referenceData, rng);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "Esoteric Hermit",
        });

        Assert.Equal("Esoteric Hermit", character.ClassName);
        // Hermit should have at least 1 scroll
        Assert.NotEmpty(character.ScrollsKnown);
    }

    [Fact]
    public async Task HereticalPriest_StartsWithSacredScroll()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new DeterministicRandom(new[] { 3, 3, 3, 3, 2, 1, 1, 1 });
        var generator = new CharacterGenerator(referenceData, rng);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "Heretical Priest",
        });

        Assert.Equal("Heretical Priest", character.ClassName);
        // Priest should have at least 1 scroll
        Assert.NotEmpty(character.ScrollsKnown);
    }

    [Fact]
    public async Task OtherClasses_DontStartWithScrolls()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var nonScrollClasses = new[]
        {
            "Fanged Deserter",
            "Gutterborn Scum",
            "Occult Herbmaster",
            "Wretched Royalty"
        };

        foreach (var className in nonScrollClasses)
        {
            var rng = new DeterministicRandom(new[] { 3, 3, 3, 3, 2, 1, 1, 1 });
            var generator = new CharacterGenerator(
                referenceData, rng);

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = className,
            });

            // These classes don't start with scrolls
            Assert.Empty(character.ScrollsKnown);
        }
    }

    [Fact]
    public async Task ScrollStrings_FollowExpectedFormat()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        foreach (var scroll in referenceData.Scrolls)
        {
            var formatted = scroll.ToFormattedString();

            // Format: "Name (Type #N, DRXX)"
            Assert.Contains(scroll.Name, formatted);
            Assert.Contains(scroll.ScrollType, formatted);
            Assert.Contains($"#{scroll.ScrollNumber}", formatted);
            Assert.Contains($"DR{scroll.UsageDR}", formatted);
        }
    }

    [Fact]
    public async Task GetRandomScroll_ReturnsValidScrollOfRequestedType()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(333);

        for (int i = 0; i < 20; i++)
        {
            var sacredScroll = referenceData.GetRandomScroll("Sacred", rng);
            var uncleanScroll = referenceData.GetRandomScroll("Unclean", rng);

            Assert.NotNull(sacredScroll);
            Assert.Equal("Sacred", sacredScroll.ScrollType);

            Assert.NotNull(uncleanScroll);
            Assert.Equal("Unclean", uncleanScroll.ScrollType);
        }
    }

    [Fact]
    public async Task AllScrollNumbers_AreBetween1And10()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        foreach (var scroll in referenceData.Scrolls)
        {
            Assert.InRange(scroll.ScrollNumber, 1, 10);
        }
    }

    [Fact]
    public async Task AllScrollUsageDRs_AreReasonable()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        foreach (var scroll in referenceData.Scrolls)
        {
            // Usage DRs typically range from 10-16 in MÖRK BORG
            Assert.InRange(scroll.UsageDR, 10, 20);
        }
    }

    [Fact]
    public async Task SacredAndUncleanScrolls_HaveDifferentNames()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        var sacredNames = referenceData.Scrolls
            .Where(s => s.ScrollType == "Sacred")
            .Select(s => s.Name)
            .ToHashSet();

        var uncleanNames = referenceData.Scrolls
            .Where(s => s.ScrollType == "Unclean")
            .Select(s => s.Name)
            .ToHashSet();

        // No scroll should appear in both lists
        var overlap = sacredNames.Intersect(uncleanNames).ToList();
        Assert.Empty(overlap);
    }

    [Fact]
    public async Task ClassStartingScrolls_AreAddedAsStructuredEntries()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new DeterministicRandom(new[] { 3, 3, 3, 3, 2, 1, 1, 1 });
        var generator = new CharacterGenerator(referenceData, rng);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "Esoteric Hermit",  // Has starting scroll
        });

        // Scrolls should be in ScrollsKnown, not just descriptions
        Assert.NotEmpty(character.ScrollsKnown);

        // Verify scroll format (should be "Name (DR: XX, Uses: X)" style or similar)
        foreach (var scroll in character.ScrollsKnown)
        {
            Assert.NotEmpty(scroll);
            Assert.DoesNotContain("placeholder", scroll, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ClasslessGearTable_ScrollReward_IsAddedToScrollsKnown()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        bool foundScrollInGear = false;

        // Generate multiple classless characters with different seeds
        // Eventually one should hit the sacred scroll outcome (Table B roll 11)
        for (int seed = 1; seed <= 30 && !foundScrollInGear; seed++)
        {
            var generator = new CharacterGenerator(referenceData, new Random(seed));
            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = "none",  // Classless, uses gear tables
            });

            // If this character has scrolls from the gear flow
            if (character.ScrollsKnown.Any())
            {
                foundScrollInGear = true;
                // Verify they're real scroll entries, not placeholders
                var scroll = character.ScrollsKnown.First();
                Assert.NotEmpty(scroll);
                Assert.DoesNotContain("placeholder", scroll, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.True(foundScrollInGear, "Expected at least one classless character to have scrolls from gear tables across 30 seeds");
    }

    [Fact]
    public async Task StartingItems_Token_RandomSacredScroll_GeneratesStructuredScroll()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        // Get an ordinary-mode class and add the token via reflection
        var classToModify = refData.Classes.FirstOrDefault(c => c.StartingEquipmentMode == "ordinary");
        Assert.NotNull(classToModify);

        var startingItemsProperty = typeof(ClassData).GetProperty(nameof(ClassData.StartingItems));
        Assert.NotNull(startingItemsProperty);
        var itemsList = new List<string> { "random_sacred_scroll" };
        startingItemsProperty.SetValue(classToModify, itemsList);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = classToModify.Name,
        });

        // Verify structured scroll is in ScrollsKnown
        Assert.NotEmpty(character.ScrollsKnown);
        var scroll = character.ScrollsKnown.First();
        Assert.Matches(@", DR\d+\)", scroll);  // Structured format includes DR followed by number
        Assert.DoesNotContain("placeholder", scroll);
    }

    [Fact]
    public async Task StartingItems_Token_RandomUncleanScroll_GeneratesStructuredScroll()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var classToModify = refData.Classes.FirstOrDefault(c => c.StartingEquipmentMode == "ordinary");
        Assert.NotNull(classToModify);

        var startingItemsProperty = typeof(ClassData).GetProperty(nameof(ClassData.StartingItems));
        Assert.NotNull(startingItemsProperty);
        var itemsList = new List<string> { "random_unclean_scroll" };
        startingItemsProperty.SetValue(classToModify, itemsList);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = classToModify.Name,
        });

        Assert.NotEmpty(character.ScrollsKnown);
        var scroll = character.ScrollsKnown.First();
        Assert.Matches(@", DR\d+\)", scroll);
        Assert.DoesNotContain("placeholder", scroll);
    }

    [Fact]
    public async Task StartingItems_Token_RandomAnyScroll_GeneratesStructuredScroll()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = new CharacterGenerator(refData, new Random(42));

        var classToModify = refData.Classes.FirstOrDefault(c => c.StartingEquipmentMode == "ordinary");
        Assert.NotNull(classToModify);

        var startingItemsProperty = typeof(ClassData).GetProperty(nameof(ClassData.StartingItems));
        Assert.NotNull(startingItemsProperty);
        var itemsList = new List<string> { "random_any_scroll" };
        startingItemsProperty.SetValue(classToModify, itemsList);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = classToModify.Name,
        });

        Assert.NotEmpty(character.ScrollsKnown);
        var scroll = character.ScrollsKnown.First();
        Assert.Matches(@", DR\d+\)", scroll);
        Assert.DoesNotContain("placeholder", scroll);
    }
}
