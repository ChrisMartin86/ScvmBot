using ScvmBot.Games.MorkBorg.Generation;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>Defines the command structure for MÖRK BORG.</summary>
public sealed class MorkBorgCommandDefinition
{
    public const string Choice3D6 = "3d6";
    public const string ChoiceFourD6Drop = "4d6-drop-lowest";
    public const string ChoiceClassNone = MorkBorgConstants.ClasslessClassName;

    public static IReadOnlyList<SubCommandDefinition> BuildSubCommands(IReadOnlyList<string> classNames)
    {
        var classChoices = new List<CommandChoice> { new("None", ChoiceClassNone) };
        foreach (var name in classNames)
            classChoices.Add(new CommandChoice(name, name));

        return new[]
        {
            new SubCommandDefinition("character", "Generate a random MÖRK BORG character", new CommandOptionDefinition[]
            {
                new("roll-method",
                    "Classless only. 4d6 drop boosts 2 random abilities. Classed always 3d6.",
                    CommandOptionType.String, Required: false,
                    Choices: new[] { new CommandChoice("3d6 (standard)", Choice3D6), new CommandChoice("4d6 drop lowest (heroic)", ChoiceFourD6Drop) }),
                new("class",
                    "Select class, 'None' for classless, or omit for random.",
                    CommandOptionType.String, Required: false,
                    Choices: classChoices),
                new("name",
                    "Override the character name",
                    CommandOptionType.String, Required: false)
            }),
            new SubCommandDefinition("party", "Generate a full adventuring party (1-4 characters, default 4)", new CommandOptionDefinition[]
            {
                new("size",
                    "Number of characters in the party (1-4, default 4)",
                    CommandOptionType.Integer, Required: false, MinValue: 1, MaxValue: 4)
            })
        };
    }
}
