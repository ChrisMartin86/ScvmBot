using Discord;
using Microsoft.Extensions.Logging;
using ScvmBot.Modules;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Orchestrates the /generate slash command.
/// Aggregates all registered <see cref="IGameModule"/> instances and routes
/// to the correct one based on the subcommand group. Rendering is delegated
/// to <see cref="RendererRegistry"/>-resolved <see cref="IResultRenderer"/> instances.
/// </summary>
public class GenerateCommandHandler : ISlashCommand
{
    private readonly IReadOnlyDictionary<string, IGameModule> _gameModules;
    private readonly RendererRegistry _rendererRegistry;
    private readonly GenerationDeliveryService _delivery;
    private readonly ILogger<GenerateCommandHandler> _logger;

    public string Name => "generate";

    public GenerateCommandHandler(
        IEnumerable<IGameModule> gameModules,
        RendererRegistry rendererRegistry,
        GenerationDeliveryService delivery,
        ILogger<GenerateCommandHandler> logger)
    {
        var modules = new Dictionary<string, IGameModule>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in gameModules)
        {
            if (!modules.TryAdd(module.CommandKey, module))
            {
                throw new InvalidOperationException(
                    $"Duplicate game module CommandKey '{module.CommandKey}': " +
                    $"'{modules[module.CommandKey].Name}' and '{module.Name}' both register the same key. " +
                    $"Each module must have a unique CommandKey.");
            }
        }
        _gameModules = modules;
        _rendererRegistry = rendererRegistry;
        _delivery = delivery;
        _logger = logger;
    }

    /// <summary>Builds the /generate command with subcommand groups from all registered game modules.</summary>
    public SlashCommandBuilder BuildCommand()
    {
        var builder = new SlashCommandBuilder()
            .WithName("generate")
            .WithDescription("Generate content in various game systems")
            .WithContextTypes(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel);

        foreach (var m in _gameModules.Values)
            builder.AddOption(m.BuildCommandGroupOptions());

        return builder;
    }

    private IGameModule ParseCommandRequest(ISlashCommandContext context)
    {
        var subcommandGroup = context.Options
            .FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommandGroup);

        if (subcommandGroup is null)
        {
            throw new InvalidOperationException("No game system specified (SubCommandGroup not found).");
        }

        var gameSystemKey = subcommandGroup.Name;
        if (!_gameModules.TryGetValue(gameSystemKey, out var gameModule))
        {
            throw new InvalidOperationException($"Unknown game system: {gameSystemKey}");
        }

        return gameModule;
    }

    private IReadOnlyCollection<IApplicationCommandInteractionDataOption>? GetSubcommandGroupOptions(
        ISlashCommandContext context)
    {
        var subcommandGroup = context.Options
            .FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommandGroup);

        return subcommandGroup?.Options;
    }

    public async Task HandleAsync(ISlashCommandContext context)
    {
        await context.DeferAsync(ephemeral: true);

        try
        {
            IGameModule gameModule;
            try
            {
                gameModule = ParseCommandRequest(context);
            }
            catch (InvalidOperationException parseEx)
            {
                await context.FollowupAsync(
                    embed: ResponseCardBuilder.Build("Error", parseEx.Message, new Color(200, 50, 50)),
                    ephemeral: true);
                return;
            }

            GenerateResult result;
            try
            {
                var subcommandGroupOptions = GetSubcommandGroupOptions(context);
                result = await gameModule.HandleGenerateCommandAsync(subcommandGroupOptions);
            }
            catch (InvalidOperationException genEx)
            {
                await context.FollowupAsync(
                    embed: ResponseCardBuilder.Build("Error", genEx.Message, new Color(200, 50, 50)),
                    ephemeral: true);
                return;
            }

            if (result is PartyGenerationResult { CharacterCount: 0 })
            {
                await context.FollowupAsync(
                    embed: ResponseCardBuilder.Build("Error", "Party generation produced no characters.", new Color(200, 50, 50)),
                    ephemeral: true);
                return;
            }

            // Render embed (required)
            var embedOutput = _rendererRegistry.RenderEmbed(result);

            // Render file attachment (optional, best-effort)
            FileOutput? fileOutput = null;
            try
            {
                fileOutput = _rendererRegistry.TryRenderFile(result);
            }
            catch (Exception pdfEx)
            {
                _logger.LogWarning(pdfEx,
                    "File rendering failed for {ResultType}; continuing without attachment.",
                    result.GetType().Name);
            }

            // Deliver
            MemoryStream? stream = null;
            FileAttachment? attachment = null;
            if (fileOutput is not null)
            {
                stream = new MemoryStream(fileOutput.Bytes);
                attachment = new FileAttachment(stream, fileOutput.FileName);
            }

            try
            {
                var isDm = context.GuildId is null;
                var followupText = isDm
                    ? (result is PartyGenerationResult ? "Here's your party!" : "Here's your character!")
                    : "Check your DMs.";
                await _delivery.DeliverAsync(context, embedOutput.Embed, attachment, followupText);
            }
            catch (Exception sendEx)
            {
                _logger.LogError(sendEx, "Failed to deliver generation result via DM");
                await context.FollowupAsync(
                    embed: ResponseCardBuilder.Build("Send Failed",
                        "Something went wrong sending your result. Please try again.", new Color(200, 50, 50)),
                    ephemeral: true);
            }
            finally
            {
                stream?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during /generate command");
            await context.FollowupAsync(
                embed: ResponseCardBuilder.Build("Generation Failed",
                    "Something went wrong. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
        }
    }
}
