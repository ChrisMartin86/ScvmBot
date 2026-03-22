using Discord;
using ScvmBot.Modules;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Converts transport-agnostic <see cref="SubCommandDefinition"/> trees into
/// Discord <see cref="SlashCommandOptionBuilder"/> trees.
/// </summary>
internal static class DiscordCommandAdapter
{
    public static SlashCommandOptionBuilder ToSlashCommandOption(
        string commandKey, string displayName, IReadOnlyList<SubCommandDefinition> subCommands)
    {
        var builder = new SlashCommandOptionBuilder()
            .WithName(commandKey)
            .WithDescription($"{displayName} game system")
            .WithType(ApplicationCommandOptionType.SubCommandGroup);

        foreach (var sub in subCommands)
        {
            var subBuilder = new SlashCommandOptionBuilder()
                .WithName(sub.Name)
                .WithDescription(sub.Description)
                .WithType(ApplicationCommandOptionType.SubCommand);

            if (sub.Options is not null)
            {
                foreach (var opt in sub.Options)
                {
                    var optBuilder = new SlashCommandOptionBuilder()
                        .WithName(opt.Name)
                        .WithDescription(opt.Description)
                        .WithType(MapOptionType(opt.Type))
                        .WithRequired(opt.Required);

                    if (opt.MinValue.HasValue) optBuilder.WithMinValue(opt.MinValue.Value);
                    if (opt.MaxValue.HasValue) optBuilder.WithMaxValue(opt.MaxValue.Value);

                    if (opt.Choices is not null)
                        foreach (var choice in opt.Choices)
                            optBuilder.AddChoice(choice.Label, choice.Value);

                    subBuilder.AddOption(optBuilder);
                }
            }

            builder.AddOption(subBuilder);
        }

        return builder;
    }

    private static ApplicationCommandOptionType MapOptionType(CommandOptionType type) => type switch
    {
        CommandOptionType.String => ApplicationCommandOptionType.String,
        CommandOptionType.Integer => ApplicationCommandOptionType.Integer,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported command option type.")
    };
}
