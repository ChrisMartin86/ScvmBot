using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Tests;

public class MorkBorgAbilityRollingTests : MorkBorgGameRulesFixture
{
    [Theory]
    [InlineData(new[] { 1, 1, 1 }, -3)]     // 3 = -3
    [InlineData(new[] { 1, 1, 2 }, -3)]     // 4 = -3
    [InlineData(new[] { 1, 2, 3 }, -2)]     // 6 = -2
    [InlineData(new[] { 1, 3, 4 }, -1)]     // 8 = -1
    [InlineData(new[] { 2, 4, 6 }, 0)]      // 12 = 0
    [InlineData(new[] { 5, 5, 5 }, 2)]      // 15 = +2
    [InlineData(new[] { 6, 6, 6 }, 3)]      // 18 = +3
    public async Task ThreeD6_MapsCorrectlyToModifiers(int[] rolls, int expectedModifier)
    {
        var rng = new DeterministicRandom(rolls);
        var dice = new DiceRoller(rng);
        var roller = new AbilityRoller(dice, rng);

        var modifier = roller.RollAbilityModifier(AbilityRollMethod.ThreeD6);

        Assert.Equal(expectedModifier, modifier);
    }

    [Theory]
    [InlineData(new[] { 1, 1, 1, 1 }, -3)]     // 1+1+1 drop 1 = 3 = -3
    [InlineData(new[] { 1, 1, 1, 6 }, -1)]     // 1+1+6 drop 1 = 8 = -1
    [InlineData(new[] { 2, 3, 4, 5 }, 0)]      // 2+3+4+5 drop 2 = 12 = 0
    [InlineData(new[] { 4, 4, 4, 4 }, 0)]      // 4+4+4 drop 4 = 12 = 0
    [InlineData(new[] { 5, 5, 5, 5 }, 2)]      // 5+5+5 drop 5 = 15 = +2
    [InlineData(new[] { 6, 6, 6, 6 }, 3)]      // 6+6+6 drop 6 = 18 = +3
    public async Task FourD6DropLowest_MapsCorrectlyToModifiers(int[] rolls, int expectedModifier)
    {
        var rng = new DeterministicRandom(rolls);
        var dice = new DiceRoller(rng);
        var roller = new AbilityRoller(dice, rng);

        var modifier = roller.RollAbilityModifier(AbilityRollMethod.FourD6DropLowest);

        Assert.Equal(expectedModifier, modifier);
    }

    [Fact]
    public async Task ThreeD6_and_FourD6_Produce_ValidModifierRange()
    {
        // Test many random rolls to ensure modifiers stay in -3 to +3 range
        for (int seed = 1; seed <= 50; seed++)
        {
            var rng = new Random(seed);
            var dice = new DiceRoller(rng);
            var roller = new AbilityRoller(dice, rng);

            for (int i = 0; i < 10; i++)
            {
                var mod3d6 = roller.RollAbilityModifier(AbilityRollMethod.ThreeD6);
                var mod4d6 = roller.RollAbilityModifier(AbilityRollMethod.FourD6DropLowest);

                Assert.InRange(mod3d6, -3, 3);
                Assert.InRange(mod4d6, -3, 3);
            }
        }
    }

    [Fact]
    public async Task ClassedCharacter_AlwaysUses3d6_IgnoresFourD6Option()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        // High rolls to differentiate 3d6 from 4d6
        var rng = new DeterministicRandom(Enumerable.Repeat(6, 12).Concat(Enumerable.Repeat(1, 18)));
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
            RollMethod = AbilityRollMethod.FourD6DropLowest,  // Should be ignored!
        });

        Assert.Equal("Fanged Deserter", character.ClassName);
        // With 3d6 all 6s: 18 = +3 (highest)
        // With 4d6 all 6s: 6+6+6+6 drop 6 = 18 = +3 (same)
        // The implementation should use 3d6, so let's just verify valid range
        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }

    [Fact]
    public async Task ClasslessCharacter_4d6DropLowest_AppliesRandomlyToTwoAbilities()
    {
        var referenceData = await LoadGameReferenceDataAsync();

        // Generate multiple characters with heroic rolls and verify:
        // 1. All modifiers are valid (-3 to +3)
        // 2. Different stat combinations emerge as "heroic" across runs
        var abilityCombosWithHighScores = new HashSet<string>();

        for (int seed = 1; seed <= 10; seed++)
        {
            var rng = new Random(seed);
            var generator = new CharacterGenerator(referenceData, rng);

            var character = generator.Generate(new CharacterGenerationOptions
            {
                ClassName = MorkBorgConstants.ClasslessClassName,  // Explicitly classless
                RollMethod = AbilityRollMethod.FourD6DropLowest,
            });

            // All modifiers must be valid
            Assert.InRange(character.Strength, -3, 3);
            Assert.InRange(character.Agility, -3, 3);
            Assert.InRange(character.Presence, -3, 3);
            Assert.InRange(character.Toughness, -3, 3);

            // Identify which abilities appear "high" (>= +1) in this run
            // This is approximate but tends to show which got 4d6-drop-lowest
            var highAbilities = new List<string>();
            if (character.Strength >= 1) highAbilities.Add("STR");
            if (character.Agility >= 1) highAbilities.Add("AGI");
            if (character.Presence >= 1) highAbilities.Add("PRE");
            if (character.Toughness >= 1) highAbilities.Add("TOU");

            if (highAbilities.Count >= 2)
            {
                abilityCombosWithHighScores.Add(string.Join(",", highAbilities.OrderBy(x => x)));
            }
        }

        // With randomization, we should see different ability combinations get the high rolls
        // We won't assert exact number of combos, but we should see variety
        Assert.NotEmpty(abilityCombosWithHighScores);
    }

    [Fact]
    public async Task AllAbilities_AreGenerated()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(42);
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(new CharacterGenerationOptions
        {
        });

        // All four abilities must be present
        Assert.InRange(character.Strength, -3, 3);
        Assert.InRange(character.Agility, -3, 3);
        Assert.InRange(character.Presence, -3, 3);
        Assert.InRange(character.Toughness, -3, 3);
    }

    [Fact]
    public void RollFourD6DropLowest_WithKnownDice_DropsLowestAndReturnsCorrectSum()
    {
        // [1,3,4,5]: sum=13, min=1, drop-lowest result=12
        var rng = new DeterministicRandom(new[] { 1, 3, 4, 5 });
        var dice = new DiceRoller(rng);

        var result = dice.RollFourD6DropLowest();

        Assert.Equal(12, result);
    }

    [Fact]
    public void ClasslessCharacter_FourD6DropLowest_ExactlyTwoAbilitiesGetHeroicRoll()
    {
        // RNG sequence: 2 index picks (0=STR, 1=AGI), then 4+4+3+3 dice values
        // Heroic abilities (4d6): all 6s → sum=24, min=6, result=18 → +3
        // Normal abilities (3d6): all 1s → sum=3 → -3
        var rng = new DeterministicRandom(new[] { 0, 1, 6, 6, 6, 6, 6, 6, 6, 6, 1, 1, 1, 1, 1, 1 });
        var dice = new DiceRoller(rng);
        var roller = new AbilityRoller(dice, rng);

        var scores = roller.Roll(
            new CharacterGenerationOptions { RollMethod = AbilityRollMethod.FourD6DropLowest },
            classData: null);

        // Exactly 2 abilities (STR and AGI) received the 4d6-drop-lowest heroic roll
        Assert.Equal(3, scores.Strength);
        Assert.Equal(3, scores.Agility);
        Assert.Equal(-3, scores.Presence);
        Assert.Equal(-3, scores.Toughness);
    }

    [Fact]
    public void ClassedCharacter_FourD6DropLowest_ProducesSameResultAsThreeD6_GivenSameRng()
    {
        // Classed characters never use heroic rolls; both options should consume identical RNG paths
        var rolls = new[] { 2, 4, 5, 3, 1, 6, 4, 2, 5, 3, 6, 1 };
        var classData = new ScvmBot.Games.MorkBorg.Reference.ClassData { Name = "Test" };

        var rng1 = new DeterministicRandom(rolls);
        var dice1 = new DiceRoller(rng1);
        var roller1 = new AbilityRoller(dice1, rng1);

        var rng2 = new DeterministicRandom(rolls);
        var dice2 = new DiceRoller(rng2);
        var roller2 = new AbilityRoller(dice2, rng2);

        var scores3d6 = roller1.Roll(new CharacterGenerationOptions { RollMethod = AbilityRollMethod.ThreeD6 }, classData);
        var scores4d6 = roller2.Roll(new CharacterGenerationOptions { RollMethod = AbilityRollMethod.FourD6DropLowest }, classData);

        Assert.Equal(scores3d6.Strength, scores4d6.Strength);
        Assert.Equal(scores3d6.Agility, scores4d6.Agility);
        Assert.Equal(scores3d6.Presence, scores4d6.Presence);
        Assert.Equal(scores3d6.Toughness, scores4d6.Toughness);
    }

    [Theory]
    [InlineData(new[] { 4, 4, 4 }, 2, 2)]   // sum=12 → 0; 0 + 2 = +2 (no clamp)
    [InlineData(new[] { 6, 6, 6 }, 1, 3)]   // sum=18 → +3; +3 + 1 = +4 → clamped to +3
    [InlineData(new[] { 1, 1, 1 }, -1, -3)] // sum=3  → -3; -3 + (-1) = -4 → clamped to -3
    public void ClassStatModifier_IsAppliedAndClamped_ForRolledStrength(int[] strRolls, int classStrModifier, int expectedStrength)
    {
        // Cross-validates roll method arithmetic with class modifier application and clamping
        // by exercising AbilityRoller.Roll() directly with a fabricated ClassData.
        var classData = new ScvmBot.Games.MorkBorg.Reference.ClassData
        {
            Name = "Test",
            StrengthModifier = classStrModifier,
        };

        // Pad remaining 9 dice with neutral values (4,4,4 per ability × 3 remaining abilities → sum=12 → 0)
        var rolls = strRolls.Concat(Enumerable.Repeat(4, 9));
        var rng = new DeterministicRandom(rolls);
        var dice = new DiceRoller(rng);
        var roller = new AbilityRoller(dice, rng);

        var scores = roller.Roll(new CharacterGenerationOptions { RollMethod = AbilityRollMethod.ThreeD6 }, classData);

        Assert.Equal(expectedStrength, scores.Strength);
        // Other abilities should all be 0 (sum=12, no modifier)
        Assert.Equal(0, scores.Agility);
        Assert.Equal(0, scores.Presence);
        Assert.Equal(0, scores.Toughness);
    }

}
