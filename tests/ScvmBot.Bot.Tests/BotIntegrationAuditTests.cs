using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Audit tests that cross the boundary between the Mork Borg game library
/// and the bot layer (command definitions, option parsing, character sheet mapping).
/// </summary>
public class BotIntegrationAuditTests
{
    private static async Task<MorkBorgReferenceDataService> LoadGameReferenceDataAsync()
    {
        var repoRoot = TestInfrastructure.GetRepositoryRoot();
        var dataPath = Path.Combine(repoRoot, "src", "ScvmBot.Games.MorkBorg", "Data");
        return await MorkBorgReferenceDataService.CreateAsync(dataPath);
    }

    #region B2 – Command definition alignment

    [Fact]
    public async Task B2_CommandDefinition_ExposesAllClasses_PlusNone()
    {
        var refData = await LoadGameReferenceDataAsync();
        var jsonClassNames = refData.Classes.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var classNames = refData.Classes.Select(c => c.Name).ToList();
        var subCommands = MorkBorgCommandDefinition.BuildSubCommands(classNames);
        var charSubcommand = subCommands
            .First(s => s.Name == "character");
        var classOption = charSubcommand.Options!
            .First(o => o.Name == "class");

        var commandChoices = classOption.Choices!
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(MorkBorgCommandDefinition.ChoiceClassNone, commandChoices);

        foreach (var name in jsonClassNames)
        {
            Assert.Contains(name, commandChoices);
        }

        var expectedCount = jsonClassNames.Count + 1; // +1 for "none"
        Assert.Equal(expectedCount, commandChoices.Count);
    }

    #endregion

    #region B3 – Parser alignment

    [Fact]
    public void B3_Parser_NoneChoice_PreservesSentinelString()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, "none", null);
        Assert.Equal("none", opts.ClassName);
    }

    [Fact]
    public void B3_Parser_NullClass_PreservesNullForRandomSelection()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, null, null);
        Assert.Null(opts.ClassName);
    }

    [Fact]
    public void B3_Parser_ExplicitClassName_PassesThroughUnchanged()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, "Fanged Deserter", null);
        Assert.Equal("Fanged Deserter", opts.ClassName);
    }

    #endregion

    #region E1 – Mapped output completeness

    [Theory]
    [InlineData("none")]
    [InlineData("Esoteric Hermit")]
    [InlineData("Fanged Deserter")]
    [InlineData("Gutterborn Scum")]
    [InlineData("Heretical Priest")]
    [InlineData("Occult Herbmaster")]
    [InlineData("Wretched Royalty")]
    public async Task E1_MappedOutput_HasPopulatedFieldsForRendering(string className)
    {
        var refData = await LoadGameReferenceDataAsync();

        for (int seed = 0; seed < 10; seed++)
        {
            var gen = new CharacterGenerator(refData, new Random(seed));
            var ch = gen.Generate(new CharacterGenerationOptions
            {
                ClassName = className,
            });

            var mapped = CharacterSheetMapper.Map(ch);

            Assert.False(string.IsNullOrWhiteSpace(mapped.Name),
                $"Seed {seed}: mapped Name must be populated");

            if (className != "none")
            {
                Assert.False(string.IsNullOrWhiteSpace(mapped.ClassName),
                    $"Seed {seed}: mapped ClassName must be populated for '{className}'");
            }

            Assert.False(string.IsNullOrWhiteSpace(mapped.HP_Current),
                $"Seed {seed}: mapped HP_Current must be populated");
            Assert.False(string.IsNullOrWhiteSpace(mapped.HP_Max),
                $"Seed {seed}: mapped HP_Max must be populated");
            Assert.True(int.TryParse(mapped.HP_Current, out var hpCur) && hpCur >= 1,
                $"Seed {seed}: HP_Current '{mapped.HP_Current}' must be a positive integer");
            Assert.True(int.TryParse(mapped.HP_Max, out var hpMax) && hpMax >= 1,
                $"Seed {seed}: HP_Max '{mapped.HP_Max}' must be a positive integer");

            Assert.False(string.IsNullOrWhiteSpace(mapped.Silver),
                $"Seed {seed}: mapped Silver must be populated");
            Assert.True(int.TryParse(mapped.Silver, out _),
                $"Seed {seed}: Silver '{mapped.Silver}' must be numeric");

            Assert.Matches(@"^[+-]\d+$", mapped.Strength);
            Assert.Matches(@"^[+-]\d+$", mapped.Agility);
            Assert.Matches(@"^[+-]\d+$", mapped.Presence);
            Assert.Matches(@"^[+-]\d+$", mapped.Toughness);

            Assert.False(string.IsNullOrWhiteSpace(mapped.Omens),
                $"Seed {seed}: mapped Omens must be populated");

            Assert.False(string.IsNullOrWhiteSpace(mapped.Description),
                $"Seed {seed}: mapped Description must be populated");

            if (ch.EquippedWeapon != null)
            {
                Assert.False(string.IsNullOrWhiteSpace(mapped.Weapons[0]),
                    $"Seed {seed}: Weapons[0] must be populated when character has weapon");
            }

            if (ch.EquippedArmor != null)
            {
                Assert.False(string.IsNullOrWhiteSpace(mapped.ArmorText),
                    $"Seed {seed}: ArmorText must be populated when character has armor");
            }

            if (ch.Items.Count > 0)
            {
                Assert.False(string.IsNullOrWhiteSpace(mapped.Equipment[0]),
                    $"Seed {seed}: Equipment[0] must be populated when character has items");
            }

            if (ch.ScrollsKnown.Count > 0)
            {
                Assert.False(string.IsNullOrWhiteSpace(mapped.Powers[0]),
                    $"Seed {seed}: Powers[0] must be populated when character has scrolls");
            }
        }
    }

    #endregion
}
