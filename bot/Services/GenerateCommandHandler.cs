using Discord;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Games;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Orchestrates the /generate slash command.
/// Aggregates all registered <see cref="IGameSystem"/> instances and routes
/// to the correct one based on the subcommand group.
/// To add a new game system, implement IGameSystem and register it in DI.
/// </summary>
public class GenerateCommandHandler : ISlashCommand
{
    private readonly IReadOnlyDictionary<string, IGameSystem> _gameSystems;
    private readonly GenerationDeliveryService _delivery;
    private readonly ILogger<GenerateCommandHandler> _logger;

    public string Name => "generate";

    public GenerateCommandHandler(
        IEnumerable<IGameSystem> gameSystems,
        GenerationDeliveryService delivery,
        ILogger<GenerateCommandHandler> logger)
    {
        _gameSystems = gameSystems.ToDictionary(gs => gs.CommandKey, StringComparer.OrdinalIgnoreCase);
        _delivery = delivery;
        _logger = logger;
    }

    /// <summary>Builds the /generate command with subcommand groups from all registered game systems.</summary>
    public SlashCommandBuilder BuildCommand()
    {
        var builder = new SlashCommandBuilder()
            .WithName("generate")
            .WithDescription("Generate content in various game systems")
            .WithContextTypes(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel);

        foreach (var gs in _gameSystems.Values)
            builder.AddOption(gs.BuildCommandGroupOptions());

        return builder;
    }

    private IGameSystem ParseCommandRequest(ISlashCommandContext context)
    {
        var subcommandGroup = context.Options
            .FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommandGroup);

        if (subcommandGroup is null)
        {
            throw new InvalidOperationException("No game system specified (SubCommandGroup not found).");
        }

        var gameSystemKey = subcommandGroup.Name;
        if (!_gameSystems.TryGetValue(gameSystemKey, out var gameSystem))
        {
            throw new InvalidOperationException($"Unknown game system: {gameSystemKey}");
        }

        return gameSystem;
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
            IGameSystem gameSystem;
            try
            {
                gameSystem = ParseCommandRequest(context);
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
                result = await gameSystem.HandleGenerateCommandAsync(subcommandGroupOptions);
            }
            catch (InvalidOperationException genEx)
            {
                await context.FollowupAsync(
                    embed: ResponseCardBuilder.Build("Error", genEx.Message, new Color(200, 50, 50)),
                    ephemeral: true);
                return;
            }

            switch (result)
            {
                case PartyGenerationResult partyResult:
                    await HandlePartyGenerationAsync(context, gameSystem, partyResult);
                    break;
                case CharacterGenerationResult charResult:
                    await HandleSingleCharacterGenerationAsync(context, gameSystem, charResult);
                    break;
                default:
                    await context.FollowupAsync(
                        embed: ResponseCardBuilder.Build("Error", "Unrecognized generation result.", new Color(200, 50, 50)),
                        ephemeral: true);
                    break;
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

    private async Task HandleSingleCharacterGenerationAsync(
        ISlashCommandContext context,
        IGameSystem gameSystem,
        CharacterGenerationResult result)
    {
        var character = result.Character;
        var embed = result.Card;

        byte[]? pdfBytes = null;
        string? fileName = null;

        var pdfSupport = gameSystem as IGamePdfSupport;
        if (pdfSupport is not null && pdfSupport.SupportsPdf)
        {
            try
            {
                pdfBytes = pdfSupport.GeneratePdf(character);
                if (pdfBytes is not null)
                    fileName = pdfSupport.BuildFileName(character);
            }
            catch (Exception pdfEx)
            {
                _logger.LogWarning(pdfEx,
                    "PDF generation failed for {GameSystem} character; continuing without PDF.",
                    gameSystem.Name);
            }
        }

        MemoryStream? stream = null;
        FileAttachment? attachment = null;
        if (pdfBytes is not null)
        {
            stream = new MemoryStream(pdfBytes);
            attachment = new FileAttachment(stream, fileName!);
        }

        try
        {
            var isDm = context.GuildId is null;
            await _delivery.DeliverAsync(context, embed, attachment,
                isDm ? "Here's your character!" : "Check your DMs.");
        }
        catch (Exception sendEx)
        {
            _logger.LogError(sendEx, "Failed to send character card via DM");
            await context.FollowupAsync(
                embed: ResponseCardBuilder.Build("Send Failed",
                    "Something went wrong sending your character. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
        }
        finally
        {
            stream?.Dispose();
        }
    }

    private async Task HandlePartyGenerationAsync(
        ISlashCommandContext context,
        IGameSystem gameSystem,
        PartyGenerationResult result)
    {
        var characters = result.Characters;

        if (characters.Count == 0)
        {
            await context.FollowupAsync(
                embed: ResponseCardBuilder.Build("Error", "Party generation produced no characters.", new Color(200, 50, 50)),
                ephemeral: true);
            return;
        }

        MemoryStream? zipStream = null;
        FileAttachment? zipAttachment = null;

        // PDF rendering and archiving are best-effort: failures here must not prevent delivery
        // of the party card embed. Each stage is isolated so one failure doesn't cascade.
        var pdfSupport = gameSystem as IGamePdfSupport;
        if (pdfSupport is not null && pdfSupport.SupportsPdf)
        {
            var memberPdfs = new List<(ICharacter Character, byte[] PdfBytes)>();
            foreach (var character in characters)
            {
                try
                {
                    var pdf = pdfSupport.GeneratePdf(character);
                    if (pdf is not null)
                        memberPdfs.Add((character, pdf));
                }
                catch (Exception pdfEx)
                {
                    _logger.LogWarning(pdfEx,
                        "PDF rendering failed for character '{CharacterName}'; skipping.",
                        character.Name);
                }
            }

            if (memberPdfs.Count > 0)
            {
                try
                {
                    var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
                    if (zipBytes.Length > 0)
                    {
                        var zipFileName = PartyZipBuilder.GeneratePartyZipFileName(result.PartyName);
                        zipStream = new MemoryStream(zipBytes);
                        zipAttachment = new FileAttachment(zipStream, zipFileName);
                    }
                }
                catch (Exception zipEx)
                {
                    _logger.LogWarning(zipEx,
                        "Failed to create party ZIP from {RenderedCount}/{TotalCount} PDF(s); continuing without archive.",
                        memberPdfs.Count, characters.Count);
                }
            }
            else
            {
                _logger.LogWarning(
                    "All {TotalCount} party member PDF(s) failed to render; delivering party card without archive.",
                    characters.Count);
            }
        }

        try
        {
            var isDm = context.GuildId is null;
            await _delivery.DeliverAsync(context, result.PartyCard, zipAttachment,
                isDm ? "Here's your party!" : "Check your DMs.");
        }
        catch (Exception sendEx)
        {
            _logger.LogError(sendEx, "Failed to send party cards to user {UserId}", context.UserId);
            await context.FollowupAsync(
                embed: ResponseCardBuilder.Build("Party Send Failed",
                    "Something went wrong sending your party. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
        }
        finally
        {
            zipStream?.Dispose();
        }
    }
}
