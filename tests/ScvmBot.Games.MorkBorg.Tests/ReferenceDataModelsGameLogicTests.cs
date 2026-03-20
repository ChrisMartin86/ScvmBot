using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Tests;

public class ReferenceDataModelsGameLogicTests
{
    [Fact]
    public void WeaponData_ToFormattedString_IncludesAllParts()
    {
        var weapon = new WeaponData
        {
            Name = "Bow",
            Damage = "d6",
            IsRanged = true,
            TwoHanded = true,
            Special = "Special Rule"
        };

        var formatted = weapon.ToFormattedString();

        Assert.Equal("Bow (Damage: d6, Ranged, Two-handed, Special Rule)", formatted);
    }

    [Fact]
    public void ArmorData_ToFormattedString_HandlesOptionalValues()
    {
        var armor = new ArmorData
        {
            Name = "Medium armor",
            Tier = 2,
            DamageReduction = "d4",
            AgilityPenalty = 2
        };

        var formatted = armor.ToFormattedString();

        Assert.Equal("Medium armor (Tier 2, DR: -d4, Defense +2 DR)", formatted);
    }

    [Fact]
    public void ArmorData_ToFormattedString_WithoutOptionalValues()
    {
        var armor = new ArmorData
        {
            Name = "No armor",
            Tier = 0
        };

        var formatted = armor.ToFormattedString();

        Assert.Equal("No armor (Tier 0)", formatted);
    }

    [Fact]
    public void ItemData_ToFormattedString_WithDescriptionAndSilver()
    {
        var item = new ItemData
        {
            Name = "Rope",
            Category = "Utility",
            Description = "30 feet",
            SilverValue = 4
        };

        var formatted = item.ToFormattedString();

        Assert.Equal("Rope (Utility, 30 feet, 4s)", formatted);
    }

    [Fact]
    public void ItemData_ToFormattedString_Minimal()
    {
        var item = new ItemData
        {
            Name = "Torch",
            Category = "Supply"
        };

        var formatted = item.ToFormattedString();

        Assert.Equal("Torch (Supply)", formatted);
    }
}

public class ClassRuleValidationTests : MorkBorgGameRulesFixture
{
    // ------------------------------------------------------------------
    // Exact stat modifier values per class (roll all 2s → base -2 each)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("Fanged Deserter", -1, -2, -3, -1)]  // STR+1, PRE-1, TOU+1
    [InlineData("Esoteric Hermit", -2, -2, -1, -2)]  // PRE+1
    [InlineData("Gutterborn Scum", -2, -1, -2, -2)]  // AGI+1
    [InlineData("Heretical Priest", -2, -2, -1, -2)]  // PRE+1
    [InlineData("Occult Herbmaster", -2, -2, -2, -1)]  // TOU+1
    [InlineData("Wretched Royalty", -3, -2, 0, -2)]   // STR-1, PRE+2
    public async Task ClassStatModifiers_ProduceExpectedAbilityScores(
        string className, int expectedStr, int expectedAgi, int expectedPre, int expectedTou)
    {
        var refData = await LoadGameReferenceDataAsync();

        // 50 x value-2 gives base score 6 → modifier -2 for every ability
        var dice = Enumerable.Repeat(2, 50).ToArray();
        var rng = new DeterministicRandom(dice);
        var gen = new CharacterGenerator(refData, rng);

        var ch = await gen.GenerateAsync(new CharacterGenerationOptions
        {
            Name = "Test",
            ClassName = className,
        });

        Assert.Equal(expectedStr, ch.Strength);
        Assert.Equal(expectedAgi, ch.Agility);
        Assert.Equal(expectedPre, ch.Presence);
        Assert.Equal(expectedTou, ch.Toughness);
    }

    // ------------------------------------------------------------------
    // Stat overrides must not be changed by class modifiers
    // ------------------------------------------------------------------

    [Fact]
    public async Task StatOverrides_AreNotModifiedByClassModifiers()
    {
        var refData = await LoadGameReferenceDataAsync();
        var dice = Enumerable.Repeat(2, 50).ToArray();
        var rng = new DeterministicRandom(dice);
        var gen = new CharacterGenerator(refData, rng);

        // Fanged Deserter: STR+1, PRE-1, TOU+1
        var ch = await gen.GenerateAsync(new CharacterGenerationOptions
        {
            Name = "Test",
            ClassName = "Fanged Deserter",
            Strength = 2,
            Toughness = 0,
        });

        Assert.Equal(2, ch.Strength);   // override kept, not +1
        Assert.Equal(0, ch.Toughness);  // override kept, not +1
        Assert.Equal(-2, ch.Agility);   // rolled, no modifier
        Assert.Equal(-3, ch.Presence);  // rolled -2, modifier -1
    }

    // ------------------------------------------------------------------
    // Occult Herbmaster custom equipment mode → Medicine chest
    // ------------------------------------------------------------------

    [Fact]
    public async Task OccultHerbmaster_GetsMedicineChest_ViaCustomEquipment()
    {
        var refData = await LoadGameReferenceDataAsync();
        var gen = new CharacterGenerator(refData, new Random(42));

        var ch = await gen.GenerateAsync(new CharacterGenerationOptions
        {
            Name = "Test",
            ClassName = "Occult Herbmaster",
        });

        Assert.Equal("Occult Herbmaster", ch.ClassName);
        Assert.Contains(ch.Items, i => i.Contains("Medicine chest", StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------
    // Weapon/armor roll die and class flag assertions on reference data
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("Fanged Deserter", "d10", "d4")]
    [InlineData("Esoteric Hermit", "d6", "d2")]
    [InlineData("Heretical Priest", "d6", "d4")]
    [InlineData("Gutterborn Scum", "d6", "d2")]
    [InlineData("Occult Herbmaster", "d6", "d2")]
    [InlineData("Wretched Royalty", "d8", "d4")]
    public async Task ClassData_HasCorrectWeaponAndArmorDice(
        string className, string expectedWeaponDie, string expectedArmorDie)
    {
        var refData = await LoadGameReferenceDataAsync();
        var classData = refData.GetClassByName(className);

        Assert.NotNull(classData);
        Assert.Equal(expectedWeaponDie, classData!.WeaponRollDie);
        Assert.Equal(expectedArmorDie, classData.ArmorRollDie);
    }

    [Theory]
    [InlineData("Fanged Deserter", false, true)]
    [InlineData("Esoteric Hermit", true, false)]
    [InlineData("Gutterborn Scum", true, false)]
    [InlineData("Heretical Priest", true, false)]
    [InlineData("Occult Herbmaster", true, false)]
    [InlineData("Wretched Royalty", true, true)]
    public async Task ClassData_HasCorrectScrollAndArmorFlags(
        string className, bool canScroll, bool canHeavyArmor)
    {
        var refData = await LoadGameReferenceDataAsync();
        var classData = refData.GetClassByName(className);

        Assert.NotNull(classData);
        Assert.Equal(canScroll, classData!.CanUseScrolls);
        Assert.Equal(canHeavyArmor, classData.CanWearHeavyArmor);
    }

    // ------------------------------------------------------------------
    // Classless characters have no stat modifiers
    // ------------------------------------------------------------------

    [Fact]
    public async Task Classless_NoStatModifiers_BaseRollsUnchanged()
    {
        var refData = await LoadGameReferenceDataAsync();
        var dice = Enumerable.Repeat(2, 50).ToArray();
        var rng = new DeterministicRandom(dice);
        var gen = new CharacterGenerator(refData, rng);

        var ch = await gen.GenerateAsync(new CharacterGenerationOptions
        {
            Name = "Test",
            ClassName = "none",
        });

        Assert.Null(ch.ClassName);
        Assert.Equal(-2, ch.Strength);
        Assert.Equal(-2, ch.Agility);
        Assert.Equal(-2, ch.Presence);
        Assert.Equal(-2, ch.Toughness);
    }

    // ------------------------------------------------------------------
    // Three-state class selection validation (end-to-end)
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExplicitClasslessState_AlwaysProducesClasslessCharacter()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Multiple runs with ClassName = "none" should always be classless
        for (int run = 0; run < 5; run++)
        {
            var gen = new CharacterGenerator(refData, new Random(100 + run));
            var ch = await gen.GenerateAsync(new CharacterGenerationOptions
            {
                Name = $"Test_{run}",
                ClassName = "none",
            });

            Assert.Null(ch.ClassName);
            Assert.Null(ch.ClassAbility);
        }
    }

    [Fact]
    public async Task OmittedClassState_ProducesBothClasslessAndClassedCharacters()
    {
        var refData = await LoadGameReferenceDataAsync();

        var classlessCount = 0;
        var classedCount = 0;

        // Multiple runs with ClassName = null should produce both states
        for (int run = 0; run < 20; run++)
        {
            var gen = new CharacterGenerator(refData, new Random(200 + run));
            var ch = await gen.GenerateAsync(new CharacterGenerationOptions
            {
                Name = $"Test_{run}",
                ClassName = null,  // Omitted, triggers random roll
            });

            if (ch.ClassName == null)
                classlessCount++;
            else
                classedCount++;
        }

        // Over 20 runs, we should see both outcomes
        Assert.True(classlessCount > 0, "Expected at least one classless outcome");
        Assert.True(classedCount > 0, "Expected at least one classed outcome");
    }

    [Fact]
    public async Task ExplicitClassState_AlwaysProducesNamedClass()
    {
        var refData = await LoadGameReferenceDataAsync();
        const string targetClass = "Fanged Deserter";

        // Multiple runs with explicit class name should always be that class
        for (int run = 0; run < 5; run++)
        {
            var gen = new CharacterGenerator(refData, new Random(300 + run));
            var ch = await gen.GenerateAsync(new CharacterGenerationOptions
            {
                Name = $"Test_{run}",
                ClassName = targetClass,
            });

            Assert.Equal(targetClass, ch.ClassName);
            Assert.NotNull(ch.ClassAbility);
        }
    }
}
