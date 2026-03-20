using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Tests;

public class MorkBorgHPAndOmensTests : MorkBorgGameRulesFixture
{
    [Theory]
    [InlineData(0, 5, 5)]   // TOU +0, d8 roll of 5 → HP = 5
    [InlineData(-1, 6, 5)]   // TOU -1, d8 roll of 6 → HP = 5
    [InlineData(-2, 8, 6)]   // TOU -2, d8 roll of 8 → HP = 6
    [InlineData(3, 1, 4)]   // TOU +3, d8 roll of 1 → HP = 4
    [InlineData(-3, 1, 1)]   // TOU -3, d8 roll of 1 → HP = max(1, -2) = 1
    public async Task HPCalculation_Toughness_Plus_HitDieRoll(int toughness, int hitDieRoll, int expectedHP)
    {
        var referenceData = await LoadGameReferenceDataAsync();

        // Supply hitDieRoll for the HP die.
        // Ability overrides bypass all 3d6 rolls, and GetRandomName uses
        // Random.Next(int) which does not route through Next(int,int) in
        // .NET 8, so the HP die is the first Next(int,int) call consumed.
        var rng = new DeterministicRandom(new[] { hitDieRoll }.Concat(Enumerable.Repeat(1, 20)));
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "",  // Classless uses d8
            Strength = 0,
            Agility = 0,
            Presence = 0,
            Toughness = toughness,
        });

        // HP = Toughness + die roll, minimum 1
        Assert.Equal(expectedHP, character.MaxHitPoints);
        Assert.Equal(expectedHP, character.HitPoints);
    }

    [Fact]
    public async Task HPMinimum_IsAlways1()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        // All dice at minimum (1) gives worst-case toughness (-3) and minimum HP die roll.
        var rng = new DeterministicRandom(Enumerable.Repeat(1, 30));
        var generator = new CharacterGenerator(referenceData, rng);

        var character = generator.Generate(new CharacterGenerationOptions
        {
            ClassName = "",
        });

        // Even with -3 TOU and 1 on d8, HP should be minimum 1
        Assert.True(character.HitPoints >= 1);
    }

    [Theory]
    [InlineData("Esoteric Hermit", 1, 4)]       // d4 omens
    [InlineData("Fanged Deserter", 1, 2)]       // d2 omens
    [InlineData("Gutterborn Scum", 1, 2)]       // d2 omens
    [InlineData("Heretical Priest", 1, 4)]      // d4 omens
    [InlineData("Occult Herbmaster", 1, 4)]     // d4 omens
    [InlineData("Wretched Royalty", 1, 2)]      // d2 omens
    public async Task Omens_WithinClassOmenDieRange(string className, int minOmens, int maxOmens)
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(555);
        var generator = new CharacterGenerator(referenceData, rng);

        for (int i = 0; i < 20; i++)
        {
            var character = generator.Generate(new CharacterGenerationOptions
            {
                ClassName = className,
            });

            Assert.InRange(character.Omens, minOmens, maxOmens);
        }
    }

    [Fact]
    public async Task Classless_UsesD8HPAndD2Omens()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(666);
        var generator = new CharacterGenerator(referenceData, rng);

        for (int i = 0; i < 20; i++)
        {
            var character = generator.Generate(new CharacterGenerationOptions
            {
                ClassName = "",  // Classless
            });

            // HP can be higher with d8 than some classes' smaller dice
            Assert.True(character.MaxHitPoints >= 1);

            // Omens must be 1 or 2 (d2)
            Assert.InRange(character.Omens, 1, 2);
        }
    }

    [Theory]
    [InlineData("Esoteric Hermit", 1, 4)]       // d4 HP
    [InlineData("Fanged Deserter", 1, 6)]       // d6 HP
    [InlineData("Gutterborn Scum", 1, 6)]       // d6 HP
    [InlineData("Heretical Priest", 1, 8)]      // d8 HP
    [InlineData("Occult Herbmaster", 1, 6)]     // d6 HP
    [InlineData("Wretched Royalty", 1, 6)]      // d6 HP
    public async Task HPDieRanges_MatchClassDefinition(string className, int minHP, int maxHP)
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(777);
        var generator = new CharacterGenerator(referenceData, rng);

        // Generate many characters to ensure die range is covered
        for (int i = 0; i < 30; i++)
        {
            var character = generator.Generate(new CharacterGenerationOptions
            {
                ClassName = className,
            });

            // HP should be within reasonable range for the class's die
            Assert.True(character.HitPoints >= minHP);
            // Maximum HP includes Toughness modifier, so we allow some variance up
            Assert.True(character.HitPoints <= maxHP + 3);  // +3 is max Toughness
        }
    }

    [Fact]
    public async Task MaxHPGreaterThanOrEqual_CurrentHP()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(888);
        var generator = new CharacterGenerator(referenceData, rng);

        for (int i = 0; i < 20; i++)
        {
            var character = generator.Generate(new CharacterGenerationOptions
            {
            });

            Assert.True(character.MaxHitPoints >= character.HitPoints);
        }
    }

    [Fact]
    public async Task AllCharactersHave_PositiveHPAndOmens()
    {
        var referenceData = await LoadGameReferenceDataAsync();
        var rng = new Random(999);
        var generator = new CharacterGenerator(referenceData, rng);

        for (int i = 0; i < 50; i++)
        {
            var character = generator.Generate(new CharacterGenerationOptions
            {
            });

            Assert.True(character.HitPoints >= 1, $"Character iteration {i}: HP should be >= 1");
            Assert.True(character.MaxHitPoints >= 1, $"Character iteration {i}: MaxHP should be >= 1");
            Assert.True(character.Omens >= 1, $"Character iteration {i}: Omens should be >= 1");
        }
    }
}
