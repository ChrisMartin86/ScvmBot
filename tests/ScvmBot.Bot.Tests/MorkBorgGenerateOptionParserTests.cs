using Discord;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Rendering.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class MorkBorgGenerateOptionParserTests
{
    [Fact]
    public void Parse_ShouldThrow_WhenSubcommandGroupIsNull()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MorkBorgGenerateOptionParser.Parse(null));

        Assert.Contains("No subcommand provided", ex.Message);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenSubcommandGroupIsEmpty()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MorkBorgGenerateOptionParser.Parse(new List<IApplicationCommandInteractionDataOption>()));

        Assert.Contains("No subcommand provided", ex.Message);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenCharacterSubcommandMissing()
    {
        // Provide a subcommand group with wrong subcommand name
        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("wrongname", ApplicationCommandOptionType.SubCommand)
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MorkBorgGenerateOptionParser.Parse(options));

        Assert.Contains("Expected 'character' subcommand was not provided", ex.Message);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenNoSubcommandTypePresent()
    {
        // Provide options that are not subcommands
        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("class", ApplicationCommandOptionType.String, "none")
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MorkBorgGenerateOptionParser.Parse(options));

        Assert.Contains("Expected 'character' subcommand was not provided", ex.Message);
    }

    [Fact]
    public void Parse_ShouldSucceed_WhenCharacterSubcommandPresent()
    {
        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand)
        };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.NotNull(result);
        Assert.Equal(AbilityRollMethod.ThreeD6, result.RollMethod);
    }

    [Fact]
    public void Parse_ShouldIgnoreOtherSubcommands_AndUseCharacter()
    {
        var characterSubcommand = CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand);
        var otherSubcommand = CreateMockSubcommand("other", ApplicationCommandOptionType.SubCommand);

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            otherSubcommand,
            characterSubcommand
        };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.NotNull(result);
        // Should parse successfully using the character subcommand
        Assert.Equal(AbilityRollMethod.ThreeD6, result.RollMethod);
    }

    [Fact]
    public void Parse_ShouldParse_RollMethodOption()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("roll-method", ApplicationCommandOptionType.String, MorkBorgCommandDefinition.ChoiceFourD6Drop)
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.Equal(AbilityRollMethod.FourD6DropLowest, result.RollMethod);
    }

    [Fact]
    public void Parse_ShouldParse_ClassOption()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("class", ApplicationCommandOptionType.String, "Fanged Deserter")
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.Equal("Fanged Deserter", result.ClassName);
    }

    [Fact]
    public void Parse_ShouldPreserve_NoneClassAs_None()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("class", ApplicationCommandOptionType.String, MorkBorgCommandDefinition.ChoiceClassNone)
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.Equal("none", result.ClassName);
    }

    [Fact]
    public void Parse_ShouldReturn_NullClassName_WhenClassIsOmitted()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("roll-method", ApplicationCommandOptionType.String, MorkBorgCommandDefinition.Choice3D6),
            CreateMockOption("name", ApplicationCommandOptionType.String, "Test Character")
            // Intentionally omitting class option
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.Null(result.ClassName);
    }

    [Fact]
    public void Parse_ShouldParse_AllOptionsSimultaneously()
    {
        var subcommandOptions = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockOption("roll-method", ApplicationCommandOptionType.String, MorkBorgCommandDefinition.ChoiceFourD6Drop),
            CreateMockOption("class", ApplicationCommandOptionType.String, "Wretched Royalty"),
            CreateMockOption("name", ApplicationCommandOptionType.String, "Gertrude"),
            CreateMockOption("persist", ApplicationCommandOptionType.Boolean, true)
        };

        var options = new List<IApplicationCommandInteractionDataOption>
        {
            CreateMockSubcommand("character", ApplicationCommandOptionType.SubCommand, subcommandOptions)
        };

        var result = MorkBorgGenerateOptionParser.Parse(options);

        Assert.Equal(AbilityRollMethod.FourD6DropLowest, result.RollMethod);
        Assert.Equal("Wretched Royalty", result.ClassName);
        Assert.Equal("Gertrude", result.Name);
    }

    // Helper methods to create simple mock Discord option structures
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
