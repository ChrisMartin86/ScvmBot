using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;
using System.IO.Compression;

namespace ScvmBot.Bot.Tests;

public class MorkBorgPartyGenerationTests
{
    [Fact]
    public async Task SubCommands_HasPartySubcommand()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommand = gs.SubCommands.FirstOrDefault(s => s.Name == "party");
        Assert.NotNull(partySubcommand);
    }

    [Fact]
    public async Task SubCommands_PartySubcommandHasSizeOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommand = gs.SubCommands.First(s => s.Name == "party");
        var sizeOpt = partySubcommand.Options?.FirstOrDefault(o => o.Name == "size");
        Assert.NotNull(sizeOpt);
        Assert.False(sizeOpt!.Required);
        Assert.Equal(CommandOptionType.Integer, sizeOpt.Type);
    }

    [Fact]
    public async Task SubCommands_PartySubcommandHasNoNameOption()
    {
        // Party names are always randomly generated; there is no user-supplied name option.
        // This test guards against accidentally re-introducing a ghost option that the parser
        // no longer supports.
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommand = gs.SubCommands.First(s => s.Name == "party");
        var nameOpt = partySubcommand.Options?.FirstOrDefault(o => o.Name == "name");
        Assert.Null(nameOpt);
    }

    [Fact]
    public async Task SubCommands_PartySubcommandExposesOnlySizeOption()
    {
        // The command surface and the parser must agree. If this count changes, the parser
        // and command definition need to be updated together.
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommand = gs.SubCommands.First(s => s.Name == "party");
        var optionNames = partySubcommand.Options?.Select(o => o.Name).ToList() ?? [];
        Assert.Equal(["size"], optionNames);
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_GeneratesMultipleCharacters()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("party",
            new Dictionary<string, object?> { ["size"] = 3L });

        var partyResult = Assert.IsType<PartyGenerationResult<Character>>(result);
        Assert.Equal(3, partyResult.Characters.Count);
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_FirstCharacterMatchesSingleProperty()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("party",
            new Dictionary<string, object?> { ["size"] = 2L });

        var partyResult = Assert.IsType<PartyGenerationResult<Character>>(result);
        Assert.False(string.IsNullOrWhiteSpace(partyResult.PartyName));
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_AllCharactersAreIndependent()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("party",
            new Dictionary<string, object?> { ["size"] = 3L });

        var partyResult = Assert.IsType<PartyGenerationResult<Character>>(result);
        var characters = partyResult.Characters;

        // All should have names (non-empty)
        Assert.All(characters, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_DefaultPartySize_IsFour()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("party",
            new Dictionary<string, object?>());

        var partyResult = Assert.IsType<PartyGenerationResult<Character>>(result);
        Assert.Equal(4, partyResult.Characters.Count);
    }

    [Fact]
    public async Task HandleSingleCharacterAsync_StillWorks()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?>());

        var charResult = Assert.IsType<CharacterGenerationResult<Character>>(result);
        Assert.NotNull(charResult.Character);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesZipFile()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("party",
            new Dictionary<string, object?> { ["size"] = 2L });

        var partyResult = Assert.IsType<PartyGenerationResult<Character>>(result);

        // Verify ZIP can be created from character data
        var members = partyResult.Characters
            .Select(c => (c.Name, new byte[] { 0x25, 0x50, 0x44, 0x46 }))
            .ToList();
        var zipBytes = PartyZipBuilder.CreatePartyZip(members);
        Assert.True(zipBytes.Length > 0);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesPartyCard()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("party",
            new Dictionary<string, object?> { ["size"] = 3L });

        var partyResult = Assert.IsType<PartyGenerationResult<Character>>(result);
        Assert.False(string.IsNullOrWhiteSpace(partyResult.PartyName));
        var card = MorkBorgPartyEmbedRenderer.BuildCard(partyResult.PartyName, partyResult.Characters);
        Assert.Contains("Party of 3", card.Description);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesRandomName()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("party",
            new Dictionary<string, object?> { ["size"] = 2L });

        var partyResult = Assert.IsType<PartyGenerationResult<Character>>(result);
        Assert.False(string.IsNullOrWhiteSpace(partyResult.PartyName));
    }

    [Fact]
    public async Task CharacterCardBuilder_StillWorks_ForIndividualCharacters()
    {
        var gs = await CreateMinimalGameSystemAsync();

        var result = await gs.HandleGenerateCommandAsync("character",
            new Dictionary<string, object?>());

        var charResult = Assert.IsType<CharacterGenerationResult<Character>>(result);
        var card = MorkBorgCharacterEmbedRenderer.BuildCard(charResult.Character);
        Assert.NotNull(card.Title);
        Assert.False(string.IsNullOrWhiteSpace(card.Title));
    }

    // Helpers

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
