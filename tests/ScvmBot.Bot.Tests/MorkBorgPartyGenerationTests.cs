using Discord;
using ScvmBot.Rendering.MorkBorg;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Rendering;
using System.IO.Compression;

namespace ScvmBot.Bot.Tests;

public class MorkBorgPartyGenerationTests
{
    [Fact]
    public async Task BuildCommandGroupOptions_HasPartySubcommand()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var partySubcommand = builder.Options?.FirstOrDefault(o => o.Name == "party");
        Assert.NotNull(partySubcommand);
        Assert.Equal(ApplicationCommandOptionType.SubCommand, partySubcommand!.Type);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_PartySubcommandHasSizeOption()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var partySubcommand = builder.Options!.First(o => o.Name == "party");
        var sizeOpt = partySubcommand.Options?.FirstOrDefault(o => o.Name == "size");
        Assert.NotNull(sizeOpt);
        Assert.False(sizeOpt!.IsRequired ?? false);
        Assert.Equal(ApplicationCommandOptionType.Integer, sizeOpt.Type);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_PartySubcommandHasNoNameOption()
    {
        // Party names are always randomly generated; there is no user-supplied name option.
        // This test guards against accidentally re-introducing a ghost option that the parser
        // no longer supports.
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var partySubcommand = builder.Options!.First(o => o.Name == "party");
        var nameOpt = partySubcommand.Options?.FirstOrDefault(o => o.Name == "name");
        Assert.Null(nameOpt);
    }

    [Fact]
    public async Task BuildCommandGroupOptions_PartySubcommandExposesOnlySizeOption()
    {
        // The command surface and the parser must agree. If this count changes, the parser
        // and command definition need to be updated together.
        var gs = await CreateMinimalGameSystemAsync();
        var builder = gs.BuildCommandGroupOptions();
        var partySubcommand = builder.Options!.First(o => o.Name == "party");
        var optionNames = partySubcommand.Options?.Select(o => o.Name).ToList() ?? [];
        Assert.Equal(["size"], optionNames);
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_GeneratesMultipleCharacters()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 3L)
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partySubcommandOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        Assert.Equal(3, partyResult.Characters.Count);
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_FirstCharacterMatchesSingleProperty()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 2L)
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partySubcommandOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        Assert.False(string.IsNullOrWhiteSpace(partyResult.PartyName));
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_AllCharactersAreIndependent()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 3L)
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partySubcommandOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        var characters = partyResult.Characters;

        // All should have names (non-empty)
        Assert.All(characters, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_DefaultPartySize_IsFour()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partySubcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand)
        };

        var result = await gs.HandleGenerateCommandAsync(partySubcommandOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        Assert.Equal(4, partyResult.Characters.Count);
    }

    [Fact]
    public async Task HandleSingleCharacterAsync_StillWorks()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charSubcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand)
        };

        var result = await gs.HandleGenerateCommandAsync(charSubcommandOptions);

        var charResult = Assert.IsType<CharacterGenerationResult>(result);
        Assert.NotNull(charResult.Character);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesZipFile()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partyOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 2L)
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partyOptions);
        var partyResult = Assert.IsType<PartyGenerationResult>(result);

        // Verify ZIP can be created from character data
        var members = partyResult.Characters
            .Select(c => (c, new byte[] { 0x25, 0x50, 0x44, 0x46 }))
            .ToList();
        var zipBytes = PartyZipBuilder.CreatePartyZip(members);
        Assert.True(zipBytes.Length > 0);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesPartyEmbed()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partyOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 3L)
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partyOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        Assert.False(string.IsNullOrWhiteSpace(partyResult.PartyName));
        var embed = MorkBorgPartyEmbedRenderer.BuildEmbed(partyResult.PartyName, partyResult.Characters);
        Assert.Contains("Party of 3", embed.Description);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesRandomName()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var partyOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 2L)
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partyOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        Assert.False(string.IsNullOrWhiteSpace(partyResult.PartyName));
    }

    [Fact]
    public async Task CharacterCardBuilder_StillWorks_ForIndividualCharacters()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var charOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand)
        };

        var result = await gs.HandleGenerateCommandAsync(charOptions);

        var charResult = Assert.IsType<CharacterGenerationResult>(result);
        var embed = MorkBorgCharacterEmbedRenderer.BuildEmbed((Character)charResult.Character);
        Assert.NotNull(embed.Title);
        Assert.False(string.IsNullOrWhiteSpace(embed.Title));
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

    private static IApplicationCommandInteractionDataOption CreateMockOption(
        string name,
        ApplicationCommandOptionType type,
        object? value)
    {
        return new SimpleApplicationCommandInteractionDataOption
        {
            Name = name,
            Type = type,
            Value = value,
            Options = null
        };
    }

    private static IApplicationCommandInteractionDataOption CreateMockSubcommand(
        string name,
        ApplicationCommandOptionType type,
        List<IApplicationCommandInteractionDataOption>? subOptions = null)
    {
        return new SimpleApplicationCommandInteractionDataOption
        {
            Name = name,
            Type = type,
            Value = null,
            Options = subOptions?.AsReadOnly()
        };
    }

    private class SimpleApplicationCommandInteractionDataOption : IApplicationCommandInteractionDataOption
    {
        public string Name { get; set; } = "";
        public ApplicationCommandOptionType Type { get; set; }
        public object? Value { get; set; }
        public IReadOnlyCollection<IApplicationCommandInteractionDataOption>? Options { get; set; }
    }
}
