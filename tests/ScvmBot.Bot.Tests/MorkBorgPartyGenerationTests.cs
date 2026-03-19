using Discord;
using ScvmBot.Bot.Games;
using ScvmBot.Bot.Games.MorkBorg;
using ScvmBot.Bot.Services;
using System.IO.Compression;

namespace ScvmBot.Bot.Tests;

public class MorkBorgPartyGenerationTests
{
    [Fact]
    public void BuildCommandGroupOptions_HasPartySubcommand()
    {
        var gs = CreateMinimalGameSystem();
        var builder = gs.BuildCommandGroupOptions();
        var partySubcommand = builder.Options?.FirstOrDefault(o => o.Name == "party");
        Assert.NotNull(partySubcommand);
        Assert.Equal(ApplicationCommandOptionType.SubCommand, partySubcommand!.Type);
    }

    [Fact]
    public void BuildCommandGroupOptions_PartySubcommandHasSizeOption()
    {
        var gs = CreateMinimalGameSystem();
        var builder = gs.BuildCommandGroupOptions();
        var partySubcommand = builder.Options!.First(o => o.Name == "party");
        var sizeOpt = partySubcommand.Options?.FirstOrDefault(o => o.Name == "size");
        Assert.NotNull(sizeOpt);
        Assert.False(sizeOpt!.IsRequired ?? false);
        Assert.Equal(ApplicationCommandOptionType.Integer, sizeOpt.Type);
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_GeneratesMultipleCharacters()
    {
        var gs = CreateMinimalGameSystem();
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
        var gs = CreateMinimalGameSystem();
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
        Assert.NotNull(partyResult.PartyCard.Title);
    }

    [Fact]
    public async Task HandlePartyGenerationAsync_AllCharactersAreIndependent()
    {
        var gs = CreateMinimalGameSystem();
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
        var gs = CreateMinimalGameSystem();
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
        var gs = CreateMinimalGameSystem();
        var charSubcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand)
        };

        var result = await gs.HandleGenerateCommandAsync(charSubcommandOptions);

        var charResult = Assert.IsType<CharacterGenerationResult>(result);
        Assert.NotNull(charResult.Character);
        Assert.NotNull(charResult.Card);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesZipFile()
    {
        var gs = CreateMinimalGameSystem();
        var partyOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 2L),
                    CreateMockOption("name", ApplicationCommandOptionType.String, "The Doomed")
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partyOptions);
        var partyResult = Assert.IsType<PartyGenerationResult>(result);

        // Verify ZIP can be created from pre-generated PDFs
        var members = partyResult.Characters
            .Select(c => (c, gs.GeneratePdf(c)!))
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
        var gs = CreateMinimalGameSystem();
        var partyOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 3L),
                    CreateMockOption("name", ApplicationCommandOptionType.String, "Test Squad")
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partyOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        Assert.Equal("Test Squad", partyResult.PartyCard.Title);
        Assert.Contains("Party of 3", partyResult.PartyCard.Description);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_UsesSuppliedPartyName()
    {
        var gs = CreateMinimalGameSystem();
        var partyOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand,
                new List<IApplicationCommandInteractionDataOption>
                {
                    CreateMockOption("size", ApplicationCommandOptionType.Integer, 2L),
                    CreateMockOption("name", ApplicationCommandOptionType.String, "Custom Name")
                })
        };

        var result = await gs.HandleGenerateCommandAsync(partyOptions);

        var partyResult = Assert.IsType<PartyGenerationResult>(result);
        Assert.Equal("Custom Name", partyResult.PartyName);
    }

    [Fact]
    public async Task HandleGenerateCommandAsync_Party_GeneratesRandomName()
    {
        var gs = CreateMinimalGameSystem();
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
        var gs = CreateMinimalGameSystem();
        var charOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand)
        };

        var result = await gs.HandleGenerateCommandAsync(charOptions);

        var charResult = Assert.IsType<CharacterGenerationResult>(result);
        Assert.NotNull(charResult.Card);
        Assert.NotNull(charResult.Card.Title);
        Assert.False(string.IsNullOrWhiteSpace(charResult.Card.Title));
    }

    // Helpers
    private static MorkBorgGameSystem CreateMinimalGameSystem()
    {
        var refData = new MorkBorgReferenceDataService();
        var generator = new CharacterGenerator(refData, new Random(42));
        return new MorkBorgGameSystem(generator);
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
