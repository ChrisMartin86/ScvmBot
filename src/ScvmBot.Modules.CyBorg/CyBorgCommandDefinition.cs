namespace ScvmBot.Modules.CyBorg;

/// <summary>Defines the command structure for Cy_Borg.</summary>
public sealed class CyBorgCommandDefinition
{
    public const string ChoiceClassNone = "none";

    public static IReadOnlyList<SubCommandDefinition> BuildSubCommands(IReadOnlyList<string> classNames)
    {
        var classChoices = new List<CommandChoice> { new("None", ChoiceClassNone) };
        foreach (var name in classNames)
            classChoices.Add(new CommandChoice(name, name));

        return new[]
        {
            new SubCommandDefinition("character", "Generate one or more random Cy_Borg characters", new CommandOptionDefinition[]
            {
                new("class",
                    "Select class, 'None' for classless, or omit for random.",
                    CommandOptionType.String, Required: false,
                    Choices: classChoices),
                new("name",
                    "Override the character name",
                    CommandOptionType.String, Required: false),
                new("count",
                    "Number of characters to generate (default 1)",
                    CommandOptionType.Integer, Required: false, MinValue: 1,
                    Role: CommandOptionRole.GenerationCount)
            })
        };
    }
}
