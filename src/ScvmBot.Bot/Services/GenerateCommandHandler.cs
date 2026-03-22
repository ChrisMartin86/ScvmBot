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
    /// <summary>
    /// Maximum character count permitted via the Discord bot, driven by Discord's
    /// file-size limits and embed rendering constraints.
    /// </summary>
    internal const int MaxDiscordCharacterCount = 4;

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
        {
            var constrained = ApplyDiscordConstraints(m.SubCommands);
            builder.AddOption(DiscordCommandAdapter.ToSlashCommandOption(m.CommandKey, m.Name, constrained));
        }

        return builder;
    }

    /// <summary>
    /// Applies Discord-specific option constraints to module command definitions.
    /// Options with <see cref="CommandOptionRole.GenerationCount"/> are capped
    /// at <see cref="MaxDiscordCharacterCount"/>.
    /// </summary>
    private static IReadOnlyList<SubCommandDefinition> ApplyDiscordConstraints(
        IReadOnlyList<SubCommandDefinition> subCommands)
    {
        return subCommands.Select(sub => sub with
        {
            Options = sub.Options?.Select(opt =>
                opt.Role == CommandOptionRole.GenerationCount
                    ? opt with { MaxValue = opt.MaxValue.HasValue
                        ? Math.Min(opt.MaxValue.Value, MaxDiscordCharacterCount)
                        : MaxDiscordCharacterCount }
                    : opt
            ).ToList()
        }).ToList();
    }

    private (IGameModule Module, string SubCommand, IReadOnlyDictionary<string, object?> Options)
        ParseCommand(ISlashCommandContext context)
    {
        var subcommandGroup = context.Options
            .FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommandGroup)
            ?? throw new InvalidOperationException("No game system specified (SubCommandGroup not found).");

        if (!_gameModules.TryGetValue(subcommandGroup.Name, out var gameModule))
            throw new InvalidOperationException($"Unknown game system: {subcommandGroup.Name}");

        var subCommand = subcommandGroup.Options
            ?.FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommand)
            ?? throw new InvalidOperationException("No subcommand found in options.");

        var options = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (subCommand.Options is not null)
        {
            foreach (var opt in subCommand.Options)
                options[opt.Name] = opt.Value;
        }

        return (gameModule, subCommand.Name, options);
    }

    private (Embed Embed, FileAttachment? Attachment, MemoryStream? Stream)
        RenderResult(GenerateResult result)
    {
        var cardOutput = _rendererRegistry.RenderCard(result);
        var embed = DiscordCardAdapter.ToEmbed(cardOutput);

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

        MemoryStream? stream = null;
        FileAttachment? attachment = null;
        if (fileOutput is not null)
        {
            stream = new MemoryStream(fileOutput.Bytes);
            attachment = new FileAttachment(stream, fileOutput.FileName);
        }

        return (embed, attachment, stream);
    }

    private async Task DeliverAndAcknowledgeAsync(
        ISlashCommandContext context,
        GenerateResult result,
        Embed embed,
        FileAttachment? attachment,
        CancellationToken ct)
    {
        bool sent;
        try
        {
            sent = await _delivery.SendResultAsync(context, embed, attachment, ct);
        }
        catch (Exception sendEx)
        {
            _logger.LogError(sendEx, "Failed to deliver generation result via DM");
            await context.FollowupAsync(
                embed: ResponseCardBuilder.Build("Send Failed",
                    "Something went wrong sending your result. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
            return;
        }

        if (!sent)
        {
            await context.FollowupAsync(
                text: "I couldn't send you a DM. Please enable DMs from server members and try again.",
                ephemeral: true);
            return;
        }

        // Result was delivered successfully. The followup acknowledgement is
        // best-effort — if it fails, the user already has their result.
        try
        {
            var followupText = context.GuildId is null
                ? (result.CharacterCount > 1 ? "Here are your characters!" : "Here's your character!")
                : "Check your DMs.";
            await context.FollowupAsync(text: followupText, ephemeral: true);
        }
        catch (Exception ackEx)
        {
            _logger.LogWarning(ackEx,
                "Result delivered successfully but followup acknowledgement failed");
        }
    }

    public async Task HandleAsync(ISlashCommandContext context, CancellationToken ct = default)
    {
        await context.DeferAsync(ephemeral: true);

        try
        {
            var (gameModule, subCommand, options) = ParseCommand(context);

            var result = await gameModule.HandleGenerateCommandAsync(subCommand, options, ct);

            if (result.CharacterCount > MaxDiscordCharacterCount)
                throw new ArgumentException(
                    $"Character count cannot exceed {MaxDiscordCharacterCount} in Discord (attachment size limits).");

            var (embed, attachment, stream) = RenderResult(result);
            try
            {
                await DeliverAndAcknowledgeAsync(context, result, embed, attachment, ct);
            }
            finally
            {
                stream?.Dispose();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            await context.FollowupAsync(
                embed: ResponseCardBuilder.Build("Error", ex.Message, new Color(200, 50, 50)),
                ephemeral: true);
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
