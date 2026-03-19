using Discord;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>Defines the Discord slash command structure for MÖRK BORG.</summary>
public sealed class MorkBorgCommandDefinition
{
    public const string Choice3D6 = "3d6";
    public const string ChoiceFourD6Drop = "4d6-drop-lowest";
    public const string ChoiceClassNone = "none";

    public static SlashCommandOptionBuilder BuildCommandGroupOptions() =>
        new SlashCommandOptionBuilder()
            .WithName("morkborg")
            .WithDescription("MÖRK BORG game system")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("character")
                .WithDescription("Generate a random MÖRK BORG character")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("roll-method")
                    .WithDescription("Classless only. 4d6 drop boosts 2 random abilities. Classed always 3d6.")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(false)
                    .AddChoice("3d6 (standard)", Choice3D6)
                    .AddChoice("4d6 drop lowest (heroic)", ChoiceFourD6Drop))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("class")
                    .WithDescription("Select class, 'None' for classless, or omit for random.")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(false)
                    .AddChoice("None", ChoiceClassNone)
                    .AddChoice("Fanged Deserter", "Fanged Deserter")
                    .AddChoice("Gutterborn Scum", "Gutterborn Scum")
                    .AddChoice("Esoteric Hermit", "Esoteric Hermit")
                    .AddChoice("Heretical Priest", "Heretical Priest")
                    .AddChoice("Occult Herbmaster", "Occult Herbmaster")
                    .AddChoice("Wretched Royalty", "Wretched Royalty"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("name")
                    .WithDescription("Override the character name")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(false)))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("party")
                .WithDescription("Generate a full adventuring party (1-4 characters, default 4)")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("size")
                    .WithDescription("Number of characters in the party (1-4, default 4)")
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithRequired(false)
                    .WithMinValue(1)
                    .WithMaxValue(4)));
}
