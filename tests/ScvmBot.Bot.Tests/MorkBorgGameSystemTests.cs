using Discord;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class MorkBorgGameSystemTests
{
    [Fact]
    public async Task BuildCommandGroupOptions_HasCorrectName()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        Assert.Equal("morkborg", builder.Name);
        Assert.Equal(ApplicationCommandOptionType.SubCommandGroup, builder.Type);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_HasCharacterSubcommand()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var characterSubcommand = builder.Options?.FirstOrDefault(o => o.Name == "character");
        Assert.NotNull(characterSubcommand);
        Assert.Equal(ApplicationCommandOptionType.SubCommand, characterSubcommand!.Type);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_CharacterSubcommandHasRollMethodOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var characterSubcommand = builder.Options!.First(o => o.Name == "character");
        var rollMethodOpt = characterSubcommand.Options?.FirstOrDefault(o => o.Name == "roll-method");
        Assert.NotNull(rollMethodOpt);
        Assert.False(rollMethodOpt!.IsRequired ?? false);
        Assert.Equal(2, rollMethodOpt.Choices?.Count);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_CharacterSubcommandHasClassOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var characterSubcommand = builder.Options!.First(o => o.Name == "character");
        var classOpt = characterSubcommand.Options?.FirstOrDefault(o => o.Name == "class");
        Assert.NotNull(classOpt);
        Assert.False(classOpt!.IsRequired ?? false);
        // "None" is always present; class choices come from reference data.
        Assert.Contains(classOpt.Choices, c => c.Value?.ToString() == MorkBorgCommandDefinition.ChoiceClassNone);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_CharacterSubcommandHasNameOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var characterSubcommand = builder.Options!.First(o => o.Name == "character");
        var nameOpt = characterSubcommand.Options?.FirstOrDefault(o => o.Name == "name");
        Assert.NotNull(nameOpt);
        Assert.False(nameOpt!.IsRequired ?? false);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_RollMethodChoices_MatchConstants()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var characterSubcommand = builder.Options!.First(o => o.Name == "character");
        var rollMethodOpt = characterSubcommand.Options!.First(o => o.Name == "roll-method");
        var values = rollMethodOpt.Choices!.Select(c => c.Value?.ToString()).ToList();
        Assert.Contains(MorkBorgCommandDefinition.Choice3D6, values);
        Assert.Contains(MorkBorgCommandDefinition.ChoiceFourD6Drop, values);
    }

    [Fact]
    public void BuildOptions_Defaults_WhenAllNull()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, null, null);

        Assert.Equal(AbilityRollMethod.ThreeD6, opts.RollMethod);
        Assert.Null(opts.Name);
    }

    [Fact]
    public void BuildOptions_FourD6DropLowest_WhenChoiceMatches()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(
            MorkBorgCommandDefinition.ChoiceFourD6Drop, null, null);

        Assert.Equal(AbilityRollMethod.FourD6DropLowest, opts.RollMethod);
    }

    [Fact]
    public void BuildOptions_ThreeD6_WhenChoiceIsDefault()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(
            MorkBorgCommandDefinition.Choice3D6, null, null);

        Assert.Equal(AbilityRollMethod.ThreeD6, opts.RollMethod);
    }

    [Fact]
    public void BuildOptions_ThreeD6_WhenChoiceIsUnknown()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions("bogus-value", null, null);
        Assert.Equal(AbilityRollMethod.ThreeD6, opts.RollMethod);
    }

    [Fact]
    public void BuildOptions_SetsNameOverride()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, null, "Karg");
        Assert.Equal("Karg", opts.Name);
    }

    [Fact]
    public void BuildOptions_SetsClassName()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, "Fanged Deserter", null);
        Assert.Equal("Fanged Deserter", opts.ClassName);
    }

    [Fact]
    public void BuildOptions_DefaultsToRandomClass_WhenNotProvided()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, null, null);
        Assert.Null(opts.ClassName);
    }

    [Fact]
    public void BuildOptions_SetsClassless_WhenNoneChosen()
    {
        var opts = MorkBorgGenerateOptionParser.ParseRawOptions(null, MorkBorgCommandDefinition.ChoiceClassNone, null);
        Assert.Equal("none", opts.ClassName);
    }

    [Fact]
    public void BuildFileName_FormatsWithName()
    {
        var character = new Character { Name = "Svein" };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.Equal("Svein.pdf", result);
    }

    [Fact]
    public void BuildFileName_SanitizesSpecialCharacters()
    {
        var character = new Character { Name = "Kärg the Wretched" };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.Equal("Kärg_the_Wretched.pdf", result);
    }

    [Fact]
    public void BuildFileName_UsesDefaultName_WhenNameIsEmpty()
    {
        var character = new Character { Name = "" };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.Equal("character.pdf", result);
    }

    [Fact]
    public void BuildFileName_UsesDefaultName_WhenNameIsWhitespace()
    {
        var character = new Character { Name = "   " };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.Equal("character.pdf", result);
    }

    [Fact]
    public void BuildFileName_PreservesHyphensAndUnderscores()
    {
        var character = new Character { Name = "half-dead_one" };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.Equal("half-dead_one.pdf", result);
    }

    [Fact]
    public void BuildFileName_ReplacesDangerousCharactersWithUnderscore()
    {
        var character = new Character { Name = "Kårg/\\:*?<>|" };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.EndsWith(".pdf", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("?", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("|", result);
    }

    [Fact]
    public void BuildFileName_TrimsPaddingUnderscores()
    {
        var character = new Character { Name = "___test___" };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.Equal("test.pdf", result);
    }

    [Fact]
    public void BuildFileName_HandlesNumbers()
    {
        var character = new Character { Name = "12345" };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.Equal("12345.pdf", result);
    }

    [Fact]
    public void BuildFileName_HandlesVerylongNames()
    {
        var longName = new string('A', 200);
        var character = new Character { Name = longName };
        var result = MorkBorgCharacterPdfRenderer.BuildFileName(character);
        Assert.EndsWith(".pdf", result);
    }

    [Fact]
    public async Task CommandKey_IsMorkborg()
    {
        var gs = await CreateMinimalGameSystemAsync();
        Assert.Equal("morkborg", gs.CommandKey);
    }

    [Fact]
    public async Task Name_IsMorkBorg()
    {
        var gs = await CreateMinimalGameSystemAsync();
        Assert.Equal("MÖRK BORG", gs.Name);
    }

    [Fact]
    public void Parse_Throws_WhenSubcommandGroupOptionsIsNull()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MorkBorgGenerateOptionParser.Parse(null));

        Assert.Contains("character", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Throws_WhenSubcommandGroupOptionsIsEmpty()
    {
        var options = new List<IApplicationCommandInteractionDataOption>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MorkBorgGenerateOptionParser.Parse(options));

        Assert.Contains("character", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Throws_WhenSubcommandNotPresent()
    {
        var mockOption = new MockApplicationCommandInteractionDataOption
        {
            Type = ApplicationCommandOptionType.String,
            Name = "not-a-subcommand",
            Value = "test"
        };

        var options = new List<IApplicationCommandInteractionDataOption> { mockOption };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MorkBorgGenerateOptionParser.Parse(options));

        Assert.Contains("character", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_SucceedsWithValidSubcommandAndNoOptions()
    {
        var mockSubcommand = new MockApplicationCommandInteractionDataOption
        {
            Type = ApplicationCommandOptionType.SubCommand,
            Name = "character",
            Options = new List<IApplicationCommandInteractionDataOption>()
        };

        var options = new List<IApplicationCommandInteractionDataOption> { mockSubcommand };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.NotNull(result);
        Assert.Equal(AbilityRollMethod.ThreeD6, result.RollMethod);
        Assert.Null(result.ClassName);
    }

    private class MockApplicationCommandInteractionDataOption : IApplicationCommandInteractionDataOption
    {
        public string Name { get; set; } = "";
        public object? Value { get; set; }
        public ApplicationCommandOptionType Type { get; set; }
        public IReadOnlyCollection<IApplicationCommandInteractionDataOption>? Options { get; set; }
    }

    private static async Task<MorkBorgModule> CreateMinimalGameSystemAsync()
    {
        var dir = TestInfrastructure.CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
        var refData = await MorkBorgReferenceDataService.CreateAsync(dir);
        var generator = new CharacterGenerator(refData, new Random(42));
        return new MorkBorgModule(generator, refData);
    }
}
