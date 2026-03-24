using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;
using System.IO.Compression;

namespace ScvmBot.Bot.Tests;

public class MorkBorgMultiCharacterGenerationTests
{
    [Fact]
    public async Task SubCommands_CharacterSubcommandHasCountOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charSubcommand = gs.SubCommands.First(s => s.Name == "character");
        var countOpt = charSubcommand.Options?.FirstOrDefault(o => o.Name == "count");
        Assert.NotNull(countOpt);
        Assert.False(countOpt!.Required);
        Assert.Equal(CommandOptionType.Integer, countOpt.Type);
        Assert.Equal(CommandOptionRole.GenerationCount, countOpt.Role);
    }

    [Fact]
    public async Task SubCommands_HasNoPartySubcommand()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var match = gs.SubCommands.FirstOrDefault(s => s.Name == "party");
        Assert.Null(match);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_CountOfThree_GeneratesThreeCharacters()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?> { ["count"] = 3L });

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Equal(3, charResult.Characters.Count);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_CountGreaterThanOne_GeneratesGroupName()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?> { ["count"] = 2L });

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.False(string.IsNullOrWhiteSpace(charResult.GroupName));
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_AllCharactersAreIndependent()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?> { ["count"] = 3L });

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.All(charResult.Characters, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_DefaultCount_IsOne()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?>());

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Single(charResult.Characters);
        Assert.Null(charResult.GroupName);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_SingleCharacter_StillWorks()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?>());

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Single(charResult.Characters);
        Assert.False(string.IsNullOrWhiteSpace(charResult.Characters[0].Name));
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_MultipleCharacters_ZipCanBeCreated()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?> { ["count"] = 2L });

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);

        var members = charResult.Characters
            .Select(c => (c.Name, new byte[] { 0x25, 0x50, 0x44, 0x46 }))
            .ToList();
        var zipBytes = CharacterZipBuilder.CreateZip(members);
        Assert.True(zipBytes.Length > 0);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_MultipleCharacters_RosterCard()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?> { ["count"] = 3L });

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        var card = MorkBorgCharacterEmbedRenderer.BuildRosterCard(
            charResult.GroupName!, charResult.Characters);
        Assert.Contains("3 Characters", card.Description);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_SingleCharacter_CharacterCard()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?>());

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        var card = MorkBorgCharacterEmbedRenderer.BuildCard(charResult.Characters[0]);
        Assert.NotNull(card.Title);
        Assert.False(string.IsNullOrWhiteSpace(card.Title));
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_NameOverride_OnlyAppliesToFirstCharacter()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?> { ["count"] = 2L, ["name"] = "CustomName" });

        var charResult = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Equal("CustomName", charResult.Characters[0].Name);
        Assert.NotEqual("CustomName", charResult.Characters[1].Name);
    }

    // Helpers

    private static async Task<MorkBorgModule> CreateMinimalGameSystemAsync()
    {
        var dir = await TestDataBuilder.CreateMinimalDataDirectoryAsync();
        var refData = await MorkBorgReferenceDataService.CreateAsync(dir);
        var generator = CharacterGeneratorFactory.Create(refData, new Random(42));
        return new MorkBorgModule(generator, refData);
    }
}
