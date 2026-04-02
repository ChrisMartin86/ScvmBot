using ScvmBot.Games.CyBorg.Generation;
using ScvmBot.Games.CyBorg.Models;
using ScvmBot.Games.CyBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.CyBorg;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Tests for the Cy_Borg module command structure, option parsing, and module identity.
/// Mirrors <see cref="MorkBorgGameSystemTests"/> for the Cy_Borg module.
/// </summary>
public class CyBorgGameSystemTests
{
    [Fact]
    public async Task SubCommands_HasCharacterSubcommand()
    {
        var gs = await CreateMinimalModuleAsync();
        var charSub = gs.SubCommands.FirstOrDefault(s => s.Name == "character");
        Assert.NotNull(charSub);
    }

    [Fact]
    public async Task SubCommands_CharacterSubcommandHasClassOption()
    {
        var gs = await CreateMinimalModuleAsync();
        var charSub = gs.SubCommands.First(s => s.Name == "character");
        var classOpt = charSub.Options?.FirstOrDefault(o => o.Name == "class");
        Assert.NotNull(classOpt);
        Assert.False(classOpt!.Required);
        Assert.Contains(classOpt.Choices!, c => c.Value == CyBorgCommandDefinition.ChoiceClassNone);
    }

    [Fact]
    public async Task SubCommands_CharacterSubcommandHasNameOption()
    {
        var gs = await CreateMinimalModuleAsync();
        var charSub = gs.SubCommands.First(s => s.Name == "character");
        var nameOpt = charSub.Options?.FirstOrDefault(o => o.Name == "name");
        Assert.NotNull(nameOpt);
        Assert.False(nameOpt!.Required);
    }

    [Fact]
    public async Task SubCommands_CharacterSubcommandHasCountOption()
    {
        var gs = await CreateMinimalModuleAsync();
        var charSub = gs.SubCommands.First(s => s.Name == "character");
        var countOpt = charSub.Options?.FirstOrDefault(o => o.Name == "count");
        Assert.NotNull(countOpt);
        Assert.False(countOpt!.Required);
        Assert.Equal(CommandOptionRole.GenerationCount, countOpt.Role);
    }

    [Fact]
    public async Task CommandKey_IsCyborg()
    {
        var gs = await CreateMinimalModuleAsync();
        Assert.Equal("cyborg", gs.CommandKey);
    }

    [Fact]
    public async Task Name_IsCyBorg()
    {
        var gs = await CreateMinimalModuleAsync();
        Assert.Equal("Cy_Borg", gs.Name);
    }

    [Fact]
    public void ParseRawOptions_Defaults_WhenAllNull()
    {
        var opts = CyBorgGenerateOptionParser.ParseRawOptions(null, null);
        Assert.Null(opts.ClassName);
        Assert.Null(opts.Name);
    }

    [Fact]
    public void ParseRawOptions_SetsClassless_WhenNoneChosen()
    {
        var opts = CyBorgGenerateOptionParser.ParseRawOptions(CyBorgCommandDefinition.ChoiceClassNone, null);
        Assert.Equal("none", opts.ClassName);
    }

    [Fact]
    public void ParseRawOptions_SetsNameOverride()
    {
        var opts = CyBorgGenerateOptionParser.ParseRawOptions(null, "GhostRun");
        Assert.Equal("GhostRun", opts.Name);
    }

    [Fact]
    public void ParseRawOptions_SetsClassName()
    {
        var opts = CyBorgGenerateOptionParser.ParseRawOptions("Street punk", null);
        Assert.Equal("Street punk", opts.ClassName);
    }

    [Fact]
    public void ParseRawOptions_DefaultsToRandomClass_WhenNotProvided()
    {
        var opts = CyBorgGenerateOptionParser.ParseRawOptions(null, null);
        Assert.Null(opts.ClassName);
    }

    // ── Integration: HandleGenerateCommandAsync through the module ─────────

    [Fact]
    public async Task HandleGenerateCommandAsync_CharacterSubcommand_ReturnsValidBatch()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));
        var module = new CyBorgModule(generator, refData);

        var options = new Dictionary<string, object?> { ["class"] = "none" };
        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var batch = Assert.IsType<GenerationBatch<CyBorgCharacter>>(result);
        Assert.Single(batch.Characters);
        Assert.False(string.IsNullOrWhiteSpace(batch.Characters[0].Name));
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_UnknownSubcommand_Throws()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));
        var module = new CyBorgModule(generator, refData);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            module.HandleGenerateCommandAsync("unknown", new Dictionary<string, object?>()));
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_MultipleCharacters_ReturnsBatchWithGroupName()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));
        var module = new CyBorgModule(generator, refData);

        var options = new Dictionary<string, object?> { ["count"] = (long)3 };
        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var batch = Assert.IsType<GenerationBatch<CyBorgCharacter>>(result);
        Assert.Equal(3, batch.Characters.Count);
        Assert.NotNull(batch.GroupName);
        Assert.False(string.IsNullOrWhiteSpace(batch.GroupName));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<CyBorgReferenceDataService> LoadRealReferenceDataAsync()
    {
        var dataPath = TestDataBuilder.GetRealCyBorgDataDirectoryPath();
        return await CyBorgReferenceDataService.CreateAsync(dataPath);
    }

    private static async Task<CyBorgModule> CreateMinimalModuleAsync()
    {
        var dir = await TestDataBuilder.CreateMinimalCyBorgDataDirectoryAsync();
        var refData = await CyBorgReferenceDataService.CreateAsync(dir);
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));
        return new CyBorgModule(generator, refData);
    }
}
