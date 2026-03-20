using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Tests;

public class ClassStatModifiersTests : MorkBorgGameRulesFixture
{
    [Fact]
    public async Task Classless_HasNoStatModifiers()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Roll all 2s: 2+2+2 = 6 total = -2 modifier (no class modifiers apply to classless)
        var diceRolls = new int[20];
        for (int i = 0; i < diceRolls.Length; i++) diceRolls[i] = 2;

        var rng = new DeterministicRandom(diceRolls);
        var generator = new CharacterGenerator(refData, rng);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "none",
        });

        // Classless should have all stats = -2 (from rolling all 2s with no modifiers)
        Assert.Equal(-2, character.Strength);
        Assert.Equal(-2, character.Agility);
        Assert.Equal(-2, character.Presence);
        Assert.Equal(-2, character.Toughness);
    }

    [Fact]
    public async Task Classed_AppliesStatModifiersCorrectly()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Roll all 2s: 2+2+2 = 6 total = -2 per ability before modifiers
        var diceRolls = new int[20];
        for (int i = 0; i < diceRolls.Length; i++) diceRolls[i] = 2;

        var rng = new DeterministicRandom(diceRolls);
        var generator = new CharacterGenerator(refData, rng);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
        });

        Assert.Equal("Fanged Deserter", character.ClassName);
        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }

    [Fact]
    public async Task StatModifiers_AreClamped_ToValidRange()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Roll all 6s: +3 per ability before modifiers
        var diceRolls = new int[20];
        for (int i = 0; i < diceRolls.Length; i++) diceRolls[i] = 6;

        var rng = new DeterministicRandom(diceRolls);
        var generator = new CharacterGenerator(refData, rng);

        var character = await generator.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
        });

        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }

    [Fact]
    public async Task MultipleClasses_CanBeGenerated_WithValidStats()
    {
        var refData = await LoadGameReferenceDataAsync();
        const int seed = 42;

        var rng1 = new Random(seed);
        var gen1 = new CharacterGenerator(refData, rng1);
        var char1 = await gen1.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
        });

        var rng2 = new Random(seed);
        var gen2 = new CharacterGenerator(refData, rng2);
        var char2 = await gen2.GenerateAsync(new CharacterGenerationOptions
        {
            ClassName = "Occult Herbmaster",
        });

        Assert.Equal("Fanged Deserter", char1.ClassName);
        Assert.Equal("Occult Herbmaster", char2.ClassName);

        Assert.InRange(char1.Strength, -3, 3);
        Assert.InRange(char1.Agility, -3, 3);
        Assert.InRange(char1.Presence, -3, 3);
        Assert.InRange(char1.Toughness, -3, 3);

        Assert.InRange(char2.Strength, -3, 3);
        Assert.InRange(char2.Agility, -3, 3);
        Assert.InRange(char2.Presence, -3, 3);
        Assert.InRange(char2.Toughness, -3, 3);
    }

    [Fact]
    public async Task AllClasses_HaveValidStats()
    {
        var refData = await LoadGameReferenceDataAsync();

        foreach (var classData in refData.Classes)
        {
            var rng = new Random(42);
            var generator = new CharacterGenerator(refData, rng);

            var character = await generator.GenerateAsync(new CharacterGenerationOptions
            {
                ClassName = classData.Name,
            });

            Assert.InRange(character.Strength, -3, 3);
            Assert.InRange(character.Agility, -3, 3);
            Assert.InRange(character.Presence, -3, 3);
            Assert.InRange(character.Toughness, -3, 3);
        }
    }
}
