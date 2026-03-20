using Discord;
using ScvmBot.Bot.Games.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class MorkBorgPartyOptionParserTests
{
    [Fact]
    public void ParsePartySize_ReturnsDefaultSize_WhenNoOptions()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(null);
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ReturnsDefaultSize_WhenEmptyOptions()
    {
        var size = MorkBorgPartyOptionParser.ParsePartySize(new List<IApplicationCommandInteractionDataOption>());
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ReturnsDefaultSize_WhenPartySubcommandAbsent()
    {
        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("wrongsubcommand", ApplicationCommandOptionType.SubCommand)
        };

        var size = MorkBorgPartyOptionParser.ParsePartySize(options);
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ReturnsDefaultSize_WhenPartySubcommandHasNoOptions()
    {
        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand)
        };

        var size = MorkBorgPartyOptionParser.ParsePartySize(options);
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ParsesLongValue_FromSize()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("size", ApplicationCommandOptionType.Integer, 3L)
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var size = MorkBorgPartyOptionParser.ParsePartySize(options);
        Assert.Equal(3, size);
    }

    [Fact]
    public void ParsePartySize_ClampsLow_BelowMinimum()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("size", ApplicationCommandOptionType.Integer, 0L)
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var size = MorkBorgPartyOptionParser.ParsePartySize(options);
        Assert.Equal(1, size);
    }

    [Fact]
    public void ParsePartySize_ClampsHigh_AboveMaximum()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("size", ApplicationCommandOptionType.Integer, 20L)
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var size = MorkBorgPartyOptionParser.ParsePartySize(options);
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_IgnoresSizeOption_WhenAbsent()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("other", ApplicationCommandOptionType.String, "value")
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var size = MorkBorgPartyOptionParser.ParsePartySize(options);
        Assert.Equal(4, size);
    }

    [Fact]
    public void ParsePartySize_ParsesLongValue_WhenValueIsInt()
    {
        // Discord always delivers integer options as long. The mock uses long to match that.
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("size", ApplicationCommandOptionType.Integer, 3L)
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("party", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var size = MorkBorgPartyOptionParser.ParsePartySize(options);
        Assert.Equal(3, size);
    }

    // Helper methods
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
