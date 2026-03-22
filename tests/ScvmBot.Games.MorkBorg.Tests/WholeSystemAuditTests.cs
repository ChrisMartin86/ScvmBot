using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;
using System.Text.RegularExpressions;

namespace ScvmBot.Games.MorkBorg.Tests;

public class WholeSystemAuditTests : MorkBorgGameRulesFixture
{
    #region A1 – Character identity invariants

    [Theory]
    [InlineData(MorkBorgConstants.ClasslessClassName)]
    [InlineData("Esoteric Hermit")]
    [InlineData("Fanged Deserter")]
    [InlineData("Gutterborn Scum")]
    [InlineData("Heretical Priest")]
    [InlineData("Occult Herbmaster")]
    [InlineData("Wretched Royalty")]
    public async Task A1_AllCharacters_HaveRequiredIdentityFields(string className)
    {
        var refData = await LoadGameReferenceDataAsync();

        for (int seed = 0; seed < 20; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions
            {
                ClassName = className,
            });

            Assert.False(string.IsNullOrWhiteSpace(ch.Name), $"Seed {seed}: Name must not be empty");
            Assert.True(ch.HitPoints >= 1, $"Seed {seed}: HP must be >= 1, got {ch.HitPoints}");
            Assert.True(ch.MaxHitPoints >= 1, $"Seed {seed}: MaxHP must be >= 1, got {ch.MaxHitPoints}");
            Assert.True(ch.Omens >= 1, $"Seed {seed}: Omens must be >= 1, got {ch.Omens}");
            Assert.InRange(ch.Strength, -3, 3);
            Assert.InRange(ch.Agility, -3, 3);
            Assert.InRange(ch.Presence, -3, 3);
            Assert.InRange(ch.Toughness, -3, 3);
            Assert.NotNull(ch.Items);
            Assert.NotNull(ch.Descriptions);
            Assert.NotNull(ch.ScrollsKnown);
        }
    }

    #endregion

    #region A2 – Ability modifier range after class modifiers

    [Fact]
    public async Task A2_AbilityModifiers_StayInRange_AfterClassModifiers()
    {
        var refData = await LoadGameReferenceDataAsync();

        foreach (var cls in refData.Classes)
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var gen = new CharacterGenerator(refData, new Random(seed));
                var ch = gen.Generate(new CharacterGenerationOptions
                {
                    ClassName = cls.Name,
                });

                Assert.InRange(ch.Strength, -3, 3);
                Assert.InRange(ch.Agility, -3, 3);
                Assert.InRange(ch.Presence, -3, 3);
                Assert.InRange(ch.Toughness, -3, 3);
            }
        }
    }

    #endregion

    #region A3 – HP invariants

    [Theory]
    [InlineData(MorkBorgConstants.ClasslessClassName, "d8")]
    [InlineData("Esoteric Hermit", "d4")]
    [InlineData("Fanged Deserter", "d6")]
    [InlineData("Gutterborn Scum", "d6")]
    [InlineData("Heretical Priest", "d8")]
    [InlineData("Occult Herbmaster", "d6")]
    [InlineData("Wretched Royalty", "d6")]
    public async Task A3_HP_NeverBelowOne_AndWithinLogicalMax(string className, string hitDie)
    {
        var refData = await LoadGameReferenceDataAsync();
        var dieMax = CharacterGenerator.ParseDieSize(hitDie);

        for (int seed = 0; seed < 50; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions
            {
                ClassName = className,
            });

            Assert.True(ch.HitPoints >= 1, $"Seed {seed}: HP={ch.HitPoints} < 1");
            Assert.True(ch.MaxHitPoints >= 1, $"Seed {seed}: MaxHP={ch.MaxHitPoints} < 1");
            // Logical max: hitDie + max toughness(+3)
            Assert.True(ch.MaxHitPoints <= dieMax + 3,
                $"Seed {seed}: MaxHP={ch.MaxHitPoints} exceeds hitDie({dieMax})+3");
            Assert.True(ch.HitPoints <= ch.MaxHitPoints,
                $"Seed {seed}: HP={ch.HitPoints} > MaxHP={ch.MaxHitPoints}");
        }
    }

    #endregion

    #region A4 – Silver formulas

    [Theory]
    [InlineData(1, 6, 10, 10, 60)]
    [InlineData(2, 6, 10, 20, 120)]
    [InlineData(1, 6, 30, 30, 180)]
    public void A4_SilverFormula_ProducesExpectedRange(int diceCount, int diceSides, int multiplier, int min, int max)
    {
        var formula = new SilverFormula(diceCount, diceSides, multiplier);
        for (int seed = 0; seed < 50; seed++)
        {
            var dice = new DiceRoller(new Random(seed));
            var result = dice.RollSilver(formula);
            Assert.InRange(result, min, max);
        }
    }

    [Fact]
    public async Task A4_AllClasses_HaveValidSilverFormulas()
    {
        var refData = await LoadGameReferenceDataAsync();

        foreach (var cls in refData.Classes)
        {
            if (cls.StartingSilver is not { } f) continue;
            Assert.True(f.DiceCount > 0, $"{cls.Name}: diceCount must be positive");
            Assert.True(f.DiceSides > 0, $"{cls.Name}: diceSides must be positive");
            Assert.True(f.Multiplier > 0, $"{cls.Name}: multiplier must be positive");
        }
    }

    [Fact]
    public async Task A4_InvalidSilverFormula_RejectedAtStartup()
    {
        var sourceDataRoot = GetDataRootPath();
        var tempDir = Path.Combine(Path.GetTempPath(), $"scvmbot-test-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);

            // Copy all data files from the real data directory
            foreach (var file in Directory.GetFiles(sourceDataRoot, "*.json"))
                File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)));

            // Overwrite classes.json with a class that has an invalid silver formula (zero dice)
            var badClassJson = """
            [
              {
                "name": "Bad Silver Class",
                "hitDie": "d8",
                "omenDie": "d2",
                "description": "test",
                "classAbility": "test",
                "startingSilver": { "diceCount": 0, "diceSides": 6, "multiplier": 10 },
                "startingEquipmentMode": "ordinary",
                "startingWeapons": [],
                "startingArmor": [],
                "startingScrolls": [],
                "startingItems": []
              }
            ]
            """;
            File.WriteAllText(Path.Combine(tempDir, "classes.json"), badClassJson);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => MorkBorgReferenceDataService.CreateAsync(tempDir));
            Assert.Contains("Bad Silver Class", ex.Message);
            Assert.Contains("invalid startingSilver", ex.Message);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region A5 – Equipment flow invariants

    [Fact]
    public async Task A5_ClasslessMode_UsesClasslessGearFlow()
    {
        var refData = await LoadGameReferenceDataAsync();
        var gen = new CharacterGenerator(refData, new Random(42));

        var ch = gen.Generate(new CharacterGenerationOptions
        {
            ClassName = MorkBorgConstants.ClasslessClassName,
        });

        Assert.Contains(ch.Items, i => i.Contains("Waterskin"));
        Assert.Contains(ch.Items, i => i.Contains("Dried food"));
    }

    [Fact]
    public async Task A5_OrdinaryMode_DoesNotUseClasslessTables()
    {
        var refData = await LoadGameReferenceDataAsync();
        var gen = new CharacterGenerator(refData, new Random(42));

        var ch = gen.Generate(new CharacterGenerationOptions
        {
            ClassName = "Fanged Deserter",
        });

        Assert.Contains(ch.Items, i => i.Contains("Waterskin"));
        Assert.Contains(ch.Items, i => i.Contains("Dried food"));
    }

    [Fact]
    public async Task A5_CustomMode_DoesNotAddBaseKit()
    {
        var refData = await LoadGameReferenceDataAsync();
        var gen = new CharacterGenerator(refData, new Random(42));

        var ch = gen.Generate(new CharacterGenerationOptions
        {
            ClassName = "Occult Herbmaster",
        });

        Assert.DoesNotContain(ch.Items, i => i.Contains("Waterskin"));
        Assert.DoesNotContain(ch.Items, i => i.Contains("Dried food"));
        Assert.Contains(ch.Items, i => i.Contains("Medicine chest"));
    }

    [Fact]
    public async Task A5_InvalidEquipmentMode_ThrowsClearly()
    {
        var refData = await LoadGameReferenceDataAsync();
        var cls = refData.Classes.First();
        var origMode = cls.StartingEquipmentMode;
        cls.StartingEquipmentMode = "invalid_test_mode";
        try
        {
            var gen = new CharacterGenerator(refData, new Random(1));
            var ex = Assert.Throws<InvalidOperationException>(
                () => gen.Generate(new CharacterGenerationOptions
                {
                    ClassName = cls.Name,
                }));
            Assert.Contains("invalid_test_mode", ex.Message);
        }
        finally
        {
            cls.StartingEquipmentMode = origMode;
        }
    }

    #endregion

    #region A6 – Scroll invariants

    [Fact]
    public async Task A6_AllScrolls_LandInScrollsKnown_NotAsItems()
    {
        var refData = await LoadGameReferenceDataAsync();
        var scrollNames = refData.Scrolls.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (int seed = 0; seed < 50; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions
            {
            });

            foreach (var scroll in ch.ScrollsKnown)
            {
                Assert.False(string.IsNullOrWhiteSpace(scroll), $"Seed {seed}: empty scroll entry");
                Assert.DoesNotContain("placeholder", scroll, StringComparison.OrdinalIgnoreCase);
                var nameMatch = scrollNames.Any(n => scroll.Contains(n, StringComparison.OrdinalIgnoreCase));
                Assert.True(nameMatch, $"Seed {seed}: scroll '{scroll}' has no matching official scroll name");
            }

            // Scroll format: "Name (Type #N, DRXX)"
            foreach (var item in ch.Items)
            {
                Assert.DoesNotMatch(@"(Sacred|Unclean) #\d+, DR\d+", item);
            }
        }
    }

    #endregion

    #region A7 – Override invariants

    [Fact]
    public async Task A7_AllOverrides_AreRespected()
    {
        var refData = await LoadGameReferenceDataAsync();
        var gen = new CharacterGenerator(refData, new Random(42));

        var opts = new CharacterGenerationOptions
        {
            Name = "Override Test",
            ClassName = "Fanged Deserter",
            Strength = 2,
            Agility = -1,
            Presence = 0,
            Toughness = 3,
            HitPoints = 7,
            MaxHitPoints = 10,
            Omens = 4,
            Silver = 99,
            WeaponName = "Sword",
            ArmorName = "Medium armor",
            StartingContainerOverride = "Sack",
            SkipRandomStartingGear = true
        };
        opts.ForceItemNames.Add("Rope");

        var ch = gen.Generate(opts);

        Assert.Equal("Override Test", ch.Name);
        Assert.Equal("Fanged Deserter", ch.ClassName);
        Assert.Equal(2, ch.Strength);
        Assert.Equal(-1, ch.Agility);
        Assert.Equal(0, ch.Presence);
        Assert.Equal(3, ch.Toughness);
        Assert.Equal(7, ch.HitPoints);
        Assert.Equal(10, ch.MaxHitPoints);
        Assert.Equal(4, ch.Omens);
        Assert.Equal(99, ch.Silver);
        Assert.NotNull(ch.EquippedWeapon);
        Assert.Contains("Sword", ch.EquippedWeapon!);
        Assert.NotNull(ch.EquippedArmor);
        Assert.Contains("Medium armor", ch.EquippedArmor!);
        Assert.Contains(ch.Items, i => i.Contains("Sack"));
        Assert.Contains(ch.Items, i => i.Contains("Rope"));
    }

    [Fact]
    public async Task A7_StatOverrides_NotModifiedByClassModifiers()
    {
        var refData = await LoadGameReferenceDataAsync();
        var dice = Enumerable.Repeat(2, 50).ToArray();
        var gen = new CharacterGenerator(refData, new DeterministicRandom(dice));

        // Wretched Royalty: STR-1, PRE+2
        var ch = gen.Generate(new CharacterGenerationOptions
        {
            ClassName = "Wretched Royalty",
            Strength = 1,
            Presence = -2,
        });

        Assert.Equal(1, ch.Strength);   // override kept, not -1
        Assert.Equal(-2, ch.Presence);  // override kept, not +2
    }

    #endregion

    #region B1 – Class configuration integrity

    private static readonly HashSet<string> AllowedDieSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "d2", "d4", "d6", "d8", "d10", "d12", "d20"
    };

    private static readonly Regex StrictDiePattern = new(@"^d\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static void AssertStrictDieString(string? die, string className, string fieldName)
    {
        Assert.False(string.IsNullOrWhiteSpace(die),
            $"{className}: {fieldName} must not be null/empty");
        Assert.Matches(StrictDiePattern, die!);
        Assert.True(AllowedDieSizes.Contains(die!),
            $"{className}: {fieldName} '{die}' is not a supported die size");
    }

    [Fact]
    public async Task B1_AllClasses_HaveValidConfiguration()
    {
        var refData = await LoadGameReferenceDataAsync();
        var supportedEquipModes = new HashSet<string> { MorkBorgConstants.EquipmentMode.Classless, MorkBorgConstants.EquipmentMode.Ordinary, MorkBorgConstants.EquipmentMode.Custom };
        var classNames = new HashSet<string>();

        foreach (var cls in refData.Classes)
        {
            Assert.False(string.IsNullOrWhiteSpace(cls.Name), "Class name must not be empty");
            Assert.DoesNotContain(cls.Name, classNames);
            classNames.Add(cls.Name);

            AssertStrictDieString(cls.HitDie, cls.Name, "hitDie");

            AssertStrictDieString(cls.OmenDie, cls.Name, "omenDie");

            if (cls.StartingSilver is { } sf)
            {
                Assert.True(sf.DiceCount > 0, $"{cls.Name}: startingSilver.diceCount must be positive");
                Assert.True(sf.DiceSides > 0, $"{cls.Name}: startingSilver.diceSides must be positive");
                Assert.True(sf.Multiplier > 0, $"{cls.Name}: startingSilver.multiplier must be positive");
            }

            if (cls.WeaponRollDie != null)
            {
                AssertStrictDieString(cls.WeaponRollDie, cls.Name, "weaponRollDie");
            }

            if (cls.ArmorRollDie != null)
            {
                AssertStrictDieString(cls.ArmorRollDie, cls.Name, "armorRollDie");
            }

            Assert.Contains(cls.StartingEquipmentMode, supportedEquipModes);

            var supportedScrollTokens = new HashSet<string> { MorkBorgConstants.ScrollToken.RandomUnclean, MorkBorgConstants.ScrollToken.RandomSacred, MorkBorgConstants.ScrollToken.RandomAnyScroll };
            foreach (var token in cls.StartingScrolls)
            {
                Assert.Contains(token, supportedScrollTokens);
            }

            foreach (var item in cls.StartingItems)
            {
                if (item.StartsWith("random_", StringComparison.OrdinalIgnoreCase))
                {
                    var supportedItemTokens = new HashSet<string>
                    {
                        MorkBorgConstants.ScrollToken.RandomSacredScroll, MorkBorgConstants.ScrollToken.RandomUncleanScroll, MorkBorgConstants.ScrollToken.RandomAnyScroll
                    };
                    Assert.Contains(item.ToLowerInvariant(), supportedItemTokens);
                }
                else
                {
                    var itemData = refData.GetItemByName(item);
                    Assert.NotNull(itemData);
                }
            }
        }
    }

    #endregion

    #region B4 – Classless vs classed isolation

    [Fact]
    public async Task B4_ClasslessGeneration_DoesNotApplyClassLogic()
    {
        var refData = await LoadGameReferenceDataAsync();
        var dice = Enumerable.Repeat(2, 50).ToArray();
        var gen = new CharacterGenerator(refData, new DeterministicRandom(dice));

        var ch = gen.Generate(new CharacterGenerationOptions
        {
            ClassName = MorkBorgConstants.ClasslessClassName,
        });

        Assert.Null(ch.ClassName);
        Assert.Null(ch.ClassAbility);
        // Stats should be unmodified base: 2+2+2 = 6 → -2
        Assert.Equal(-2, ch.Strength);
        Assert.Equal(-2, ch.Agility);
        Assert.Equal(-2, ch.Presence);
        Assert.Equal(-2, ch.Toughness);
    }

    [Fact]
    public async Task B4_ClassedGeneration_DoesNotUseClasslessTables()
    {
        var refData = await LoadGameReferenceDataAsync();

        var tableAOnlyItems = new[] { "Bear trap", "Bomb", "Red poison", "Magnesium strip" };

        foreach (var cls in refData.Classes.Where(c => c.StartingEquipmentMode == "ordinary"))
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var gen = new CharacterGenerator(refData, new Random(seed));
                var ch = gen.Generate(new CharacterGenerationOptions
                {
                    ClassName = cls.Name,
                });

                foreach (var exclusive in tableAOnlyItems)
                {
                    Assert.DoesNotContain(ch.Items, i => i.Contains(exclusive, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }

    #endregion

    #region C1 – Item references

    [Fact]
    public async Task C1_AllGearTableItems_ExistInItemsJson()
    {
        var refData = await LoadGameReferenceDataAsync();

        // Table A items (d12)
        var tableAItems = new[]
        {
            "Rope", "Torch", "Oil lamp", "Lantern oil", "Magnesium strip",
            "Medicine chest", "Metal file", "Lockpicks", "Bear trap",
            "Bomb", "Red poison", "Life elixir", "Heavy chain", "Grappling hook"
        };

        foreach (var name in tableAItems)
        {
            Assert.NotNull(refData.GetItemByName(name));
        }

        // Table B items (d12)
        var tableBItems = new[]
        {
            "Small vicious dog", "Life elixir", "Exquisite perfume",
            "Toolbox", "Heavy chain", "Grappling hook", "Shield",
            "Crowbar", "Lard", "Tent"
        };

        foreach (var name in tableBItems)
        {
            Assert.NotNull(refData.GetItemByName(name));
        }

        // Container items
        var containerItems = new[] { "Backpack", "Sack", "Small wagon", "Mule" };
        foreach (var name in containerItems)
        {
            Assert.NotNull(refData.GetItemByName(name));
        }

        // Base kit items
        Assert.NotNull(refData.GetItemByName("Waterskin"));
        Assert.NotNull(refData.GetItemByName("Dried food"));
    }

    [Fact]
    public async Task C1_AllClassStartingItems_ExistInItemsJson()
    {
        var refData = await LoadGameReferenceDataAsync();

        foreach (var cls in refData.Classes)
        {
            foreach (var item in cls.StartingItems)
            {
                if (item.StartsWith("random_", StringComparison.OrdinalIgnoreCase))
                    continue; // tokens, not item names

                var found = refData.GetItemByName(item);
                Assert.NotNull(found);
            }
        }
    }

    #endregion

    #region C2 – Scroll pools

    [Fact]
    public async Task C2_SacredAndUnclean_PoolsAreNonEmpty()
    {
        var refData = await LoadGameReferenceDataAsync();

        var sacred = refData.Scrolls.Where(s => s.Kind == ScrollKind.Sacred).ToList();
        var unclean = refData.Scrolls.Where(s => s.Kind == ScrollKind.Unclean).ToList();

        Assert.NotEmpty(sacred);
        Assert.NotEmpty(unclean);
        Assert.Equal(10, sacred.Count);
        Assert.Equal(10, unclean.Count);
    }

    [Fact]
    public async Task C2_RandomScroll_NeverReturnsNull()
    {
        var refData = await LoadGameReferenceDataAsync();
        var rng = new Random(42);

        for (int i = 0; i < 50; i++)
        {
            Assert.NotNull(refData.GetRandomScroll(ScrollKind.Sacred, rng));
            Assert.NotNull(refData.GetRandomScroll(ScrollKind.Unclean, rng));
        }
    }

    #endregion

    #region C3 – Weapon and armor references

    [Fact]
    public async Task C3_WeaponTable_AllEntriesExistInWeaponsJson()
    {
        var refData = await LoadGameReferenceDataAsync();

        // The d10 weapon table from CharacterGenerator.ResolveWeapon
        var tableWeapons = new[]
        {
            "Femur", "Staff", "Shortsword", "Knife", "Warhammer",
            "Sword", "Bow", "Flail", "Crossbow", "Zweihänder"
        };

        foreach (var name in tableWeapons)
        {
            Assert.NotNull(refData.GetWeaponByName(name));
        }
    }

    [Fact]
    public async Task C3_ArmorTiers_AllExist()
    {
        var refData = await LoadGameReferenceDataAsync();

        for (int tier = 0; tier <= 3; tier++)
        {
            Assert.NotNull(refData.GetArmorByTier(tier));
        }
    }

    [Fact]
    public async Task C3_ClassStartingWeapons_ExistInWeaponsJson()
    {
        var refData = await LoadGameReferenceDataAsync();

        foreach (var cls in refData.Classes)
        {
            foreach (var weaponName in cls.StartingWeapons)
            {
                Assert.NotNull(refData.GetWeaponByName(weaponName));
            }
        }
    }

    [Fact]
    public async Task C3_ClassStartingArmor_ExistInArmorJson()
    {
        var refData = await LoadGameReferenceDataAsync();

        foreach (var cls in refData.Classes)
        {
            foreach (var armorName in cls.StartingArmor)
            {
                Assert.NotNull(refData.GetArmorByName(armorName));
            }
        }
    }

    #endregion

    #region C4 – Description tables

    [Fact]
    public async Task C4_RequiredDescriptionTables_AreNonEmpty()
    {
        var refData = await LoadGameReferenceDataAsync();
        var rng = new Random(42);

        Assert.False(string.IsNullOrEmpty(refData.GetRandomTrait(rng)), "Trait table returned empty");
        Assert.False(string.IsNullOrEmpty(refData.GetRandomBody(rng)), "BrokenBody table returned empty");
        Assert.False(string.IsNullOrEmpty(refData.GetRandomHabit(rng)), "BadHabit table returned empty");
    }

    [Fact]
    public async Task C4_DescriptionTables_ContainEntries()
    {
        var refData = await LoadGameReferenceDataAsync();

        Assert.NotEmpty(refData.Descriptions.Trait);
        Assert.NotEmpty(refData.Descriptions.BrokenBody);
        Assert.NotEmpty(refData.Descriptions.BadHabit);
    }

    #endregion

    #region C5 – Names integrity

    [Fact]
    public async Task C5_Names_NeverReturnEmpty()
    {
        var refData = await LoadGameReferenceDataAsync();
        var rng = new Random(42);

        for (int i = 0; i < 100; i++)
        {
            var name = refData.GetRandomName(rng);
            Assert.False(string.IsNullOrWhiteSpace(name), $"Iteration {i}: name was empty");
        }
    }

    [Fact]
    public async Task C5_AllNames_AreValid()
    {
        var refData = await LoadGameReferenceDataAsync();

        foreach (var name in refData.Names)
        {
            Assert.False(string.IsNullOrWhiteSpace(name));
        }
    }

    #endregion

    #region E1 – Output completeness

    [Fact]
    public async Task E1_GeneratedCharacter_HasAllFieldsForDisplay()
    {
        var refData = await LoadGameReferenceDataAsync();

        for (int seed = 0; seed < 20; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions());

            Assert.NotEmpty(ch.Name);
            Assert.True(ch.HitPoints >= 1);
            Assert.True(ch.MaxHitPoints >= 1);
            Assert.True(ch.Omens >= 1);
            Assert.True(ch.Silver >= 0);
            Assert.NotNull(ch.Items);
            Assert.NotNull(ch.Descriptions);
            Assert.NotEmpty(ch.Descriptions); // At least Trait/Body/Habit
            Assert.NotNull(ch.ScrollsKnown);
        }
    }

    [Fact]
    public async Task E1_AllCharacters_HaveTraitBodyHabit()
    {
        var refData = await LoadGameReferenceDataAsync();

        for (int seed = 0; seed < 20; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions());

            Assert.Contains(ch.Descriptions, d => d.Category == DescriptionCategory.Trait);
            Assert.Contains(ch.Descriptions, d => d.Category == DescriptionCategory.Body);
            Assert.Contains(ch.Descriptions, d => d.Category == DescriptionCategory.Habit);
        }
    }

    #endregion

    #region E2 – Scroll formatting

    [Fact]
    public async Task E2_ScrollFormatting_IsStructured()
    {
        var refData = await LoadGameReferenceDataAsync();

        var scrollClasses = new[] { "Esoteric Hermit", "Heretical Priest" };

        foreach (var className in scrollClasses)
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var gen = new CharacterGenerator(refData, new Random(seed));
                var ch = gen.Generate(new CharacterGenerationOptions
                {
                    ClassName = className,
                });

                foreach (var scroll in ch.ScrollsKnown)
                {
                    // Format: "Name (Type #N, DRXX)"
                    Assert.Matches(@"DR\d+\)", scroll);
                    Assert.Matches(@"#\d+", scroll);
                }
            }
        }
    }

    #endregion

    #region E3 – Equipment output stability

    [Fact]
    public async Task E3_WeaponAndArmor_FormattedStrings_AreUsable()
    {
        var refData = await LoadGameReferenceDataAsync();

        for (int seed = 0; seed < 20; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions());

            if (ch.EquippedWeapon != null)
            {
                Assert.True(ch.EquippedWeapon.Length > 3);
                Assert.Contains("Damage:", ch.EquippedWeapon);
            }

            if (ch.EquippedArmor != null)
            {
                Assert.True(ch.EquippedArmor.Length > 3);
                Assert.Contains("Tier", ch.EquippedArmor);
            }
        }
    }

    [Fact]
    public async Task E3_ItemStrings_AreNonEmpty()
    {
        var refData = await LoadGameReferenceDataAsync();

        for (int seed = 0; seed < 20; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions());

            foreach (var item in ch.Items)
            {
                Assert.False(string.IsNullOrWhiteSpace(item), $"Seed {seed}: empty item string");
            }
        }
    }

    #endregion
}
