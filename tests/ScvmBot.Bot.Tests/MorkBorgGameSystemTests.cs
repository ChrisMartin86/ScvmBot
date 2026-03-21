using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class MorkBorgGameSystemTests
{
    [Fact]
    public async Task SubCommands_HasCharacterSubcommand()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charSub = gs.SubCommands.FirstOrDefault(s => s.Name == "character");
        Assert.NotNull(charSub);
    }

    [Fact]
    public async Task SubCommands_CharacterSubcommandHasRollMethodOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charSub = gs.SubCommands.First(s => s.Name == "character");
        var rollMethodOpt = charSub.Options?.FirstOrDefault(o => o.Name == "roll-method");
        Assert.NotNull(rollMethodOpt);
        Assert.False(rollMethodOpt!.Required);
        Assert.Equal(2, rollMethodOpt.Choices?.Count);
    }

    [Fact]
    public async Task SubCommands_CharacterSubcommandHasClassOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charSub = gs.SubCommands.First(s => s.Name == "character");
        var classOpt = charSub.Options?.FirstOrDefault(o => o.Name == "class");
        Assert.NotNull(classOpt);
        Assert.False(classOpt!.Required);
        // "None" is always present; class choices come from reference data.
        Assert.Contains(classOpt.Choices!, c => c.Value == MorkBorgCommandDefinition.ChoiceClassNone);
    }

    [Fact]
    public async Task SubCommands_CharacterSubcommandHasNameOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charSub = gs.SubCommands.First(s => s.Name == "character");
        var nameOpt = charSub.Options?.FirstOrDefault(o => o.Name == "name");
        Assert.NotNull(nameOpt);
        Assert.False(nameOpt!.Required);
    }

    [Fact]
    public async Task SubCommands_RollMethodChoices_MatchConstants()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charSub = gs.SubCommands.First(s => s.Name == "character");
        var rollMethodOpt = charSub.Options!.First(o => o.Name == "roll-method");
        var values = rollMethodOpt.Choices!.Select(c => c.Value).ToList();
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
