using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Tests;

/// <summary>
/// Deterministic tests that exercise every d12 roll outcome for the classless
/// starting equipment tables A and B, plus the random_any_scroll token path.
/// </summary>
public class ClasslessGearTableRollTests : MorkBorgGameRulesFixture
{
    [Theory]
    [InlineData(1, "Rope")]
    [InlineData(2, "Torch")]
    [InlineData(3, "Oil lamp")]
    [InlineData(4, "Magnesium strip")]
    [InlineData(5, "Medicine chest")]
    [InlineData(6, "Metal file")]
    [InlineData(7, "Bear trap")]
    [InlineData(8, "Bomb")]
    [InlineData(9, "Red poison")]
    [InlineData(10, "Life elixir")]
    [InlineData(11, "Heavy chain")]
    [InlineData(12, "Grappling hook")]
    public async Task TableA_Roll_ProducesExpectedItem(int roll, string expectedItem)
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(1));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = MorkBorgConstants.ClasslessClassName,
            StartingGearRollA = roll,
            StartingGearRollB = 10,    // Lard — simple item with no sub-rolls
        });

        Assert.Contains(character.Items, i => i.Contains(expectedItem, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1, "Small vicious dog")]
    [InlineData(3, "Life elixir")]
    [InlineData(4, "Exquisite perfume")]
    [InlineData(5, "Toolbox")]
    [InlineData(6, "Heavy chain")]
    [InlineData(7, "Grappling hook")]
    [InlineData(8, "Shield")]
    [InlineData(9, "Crowbar")]
    [InlineData(10, "Lard")]
    [InlineData(12, "Tent")]
    public async Task TableB_Roll_ProducesExpectedItem(int roll, string expectedItem)
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(1));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = MorkBorgConstants.ClasslessClassName,
            StartingGearRollA = 1,     // Rope — simple item with no sub-rolls
            StartingGearRollB = roll,
        });

        Assert.Contains(character.Items, i => i.Contains(expectedItem, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TableB_Roll2_ProducesMonkeyDescription()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(1));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = MorkBorgConstants.ClasslessClassName,
            StartingGearRollA = 1,
            StartingGearRollB = 2,   // Monkeys — description only, no item
        });

        Assert.Contains(character.Descriptions, d => d.Text.Contains("monkeys", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TableB_Roll11_ProducesSacredScroll()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(1));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = MorkBorgConstants.ClasslessClassName,
            StartingGearRollA = 1,
            StartingGearRollB = 11,  // Sacred scroll
        });

        // Should have at least one scroll from the Sacred list
        Assert.NotEmpty(character.ScrollsKnown);
    }

    [Fact]
    public async Task SkipRandomStartingGear_ProducesNoGearTableItems()
    {
        var refData = await LoadGameReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(42));

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = MorkBorgConstants.ClasslessClassName,
            SkipRandomStartingGear = true,
        });

        // Should still have basic items (waterskin, food) but nothing from d12 tables
        Assert.Contains(character.Items, i => i.Contains("Waterskin"));
        Assert.Contains(character.Items, i => i.Contains("Dried food"));

        // No gear table-only items
        var gearTableOnlyItems = new[] { "Bomb", "Red poison", "Bear trap", "Magnesium strip", "Medicine chest", "Exquisite perfume", "Lard", "Tent" };
        foreach (var item in gearTableOnlyItems)
        {
            Assert.DoesNotContain(character.Items, i => i.Contains(item, StringComparison.OrdinalIgnoreCase));
        }
    }
}
