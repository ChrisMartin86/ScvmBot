using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Games;
using ScvmBot.Bot.Models;
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
    private readonly ILogger<GenerateCommandHandler> _logger;

    public string Name => "generate";

    public GenerateCommandHandler(
        IEnumerable<IGameSystem> gameSystems,
        ILogger<GenerateCommandHandler> logger)
    {
        _gameSystems = gameSystems.ToDictionary(gs => gs.CommandKey, StringComparer.OrdinalIgnoreCase);
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

    /// <summary>Returns true when the interaction originated from a DM (no guild context).</summary>
    internal static bool IsDmInteraction(ulong? guildId) => guildId is null;

    /// <summary>Returns the appropriate followup text based on whether the interaction is in a DM or guild.</summary>
    internal static string GetFollowupText(bool isDm) =>
        isDm ? "Here's your character!" : "Check your DMs.";

    /// <summary>Returns the appropriate party followup text based on whether the interaction is in a DM or guild.</summary>
    internal static string GetPartyFollowupText(bool isDm) =>
        isDm ? "Here's your party!" : "Check your DMs.";

    private IGameSystem ParseCommandRequest(SocketSlashCommand command)
    {
        var subcommandGroup = command.Data.Options
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
        SocketSlashCommand command)
    {
        var subcommandGroup = command.Data.Options
            .FirstOrDefault(o => o.Type == ApplicationCommandOptionType.SubCommandGroup);

        return subcommandGroup?.Options;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
        Justification = "Discord socket wiring; SocketSlashCommand cannot be constructed in unit tests.")]
    public async Task HandleAsync(SocketSlashCommand command)
    {
        await command.DeferAsync(ephemeral: true);

        try
        {
            IGameSystem gameSystem;
            try
            {
                gameSystem = ParseCommandRequest(command);
            }
            catch (InvalidOperationException parseEx)
            {
                await command.FollowupAsync(
                    embed: ResponseCardBuilder.Build("Error", parseEx.Message, new Color(200, 50, 50)),
                    ephemeral: true);
                return;
            }

            GenerateResult result;
            try
            {
                var subcommandGroupOptions = GetSubcommandGroupOptions(command);
                result = await gameSystem.HandleGenerateCommandAsync(subcommandGroupOptions);
            }
            catch (InvalidOperationException genEx)
            {
                await command.FollowupAsync(
                    embed: ResponseCardBuilder.Build("Error", genEx.Message, new Color(200, 50, 50)),
                    ephemeral: true);
                return;
            }

            switch (result)
            {
                case PartyGenerationResult partyResult:
                    await HandlePartyGenerationAsync(command, gameSystem, partyResult);
                    break;
                case CharacterGenerationResult charResult:
                    await HandleSingleCharacterGenerationAsync(command, gameSystem, charResult);
                    break;
                default:
                    await command.FollowupAsync(
                        embed: ResponseCardBuilder.Build("Error", "Unrecognized generation result.", new Color(200, 50, 50)),
                        ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during /generate command");
            await command.FollowupAsync(
                embed: ResponseCardBuilder.Build("Generation Failed",
                    "Something went wrong. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
        }
    }

    private async Task HandleSingleCharacterGenerationAsync(
        SocketSlashCommand command,
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

        try
        {
            var isDm = IsDmInteraction(command.GuildId);
            IMessageChannel targetChannel = isDm
                ? command.Channel
                : await command.User.CreateDMChannelAsync();

            if (pdfBytes is not null)
            {
                using var stream = new MemoryStream(pdfBytes);
                var attachment = new FileAttachment(stream, fileName!);
                await targetChannel.SendFileAsync(attachment, embed: embed);
            }
            else
            {
                await targetChannel.SendMessageAsync(embed: embed);
            }

            await command.FollowupAsync(text: GetFollowupText(isDm), ephemeral: true);
        }
        catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            _logger.LogWarning(httpEx, "Cannot DM user {UserId}", command.User.Id);
            await command.FollowupAsync(
                text: "I couldn't send you a DM. Please enable DMs from server members and try again.",
                ephemeral: true);
        }
        catch (Exception sendEx)
        {
            _logger.LogError(sendEx, "Failed to send character card via DM");
            await command.FollowupAsync(
                embed: ResponseCardBuilder.Build("Send Failed",
                    "Something went wrong sending your character. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
        }
    }

    private async Task HandlePartyGenerationAsync(
        SocketSlashCommand command,
        IGameSystem gameSystem,
        PartyGenerationResult result)
    {
        var characters = result.Characters;

        if (characters.Count == 0)
        {
            await command.FollowupAsync(
                embed: ResponseCardBuilder.Build("Error", "Party generation produced no characters.", new Color(200, 50, 50)),
                ephemeral: true);
            return;
        }

        try
        {
            FileAttachment? zipAttachment = null;
            MemoryStream? zipStream = null;

            var pdfSupport = gameSystem as IGamePdfSupport;
            if (pdfSupport is not null && pdfSupport.SupportsPdf)
            {
                var memberPdfs = new List<(ICharacter Character, byte[] PdfBytes)>();
                foreach (var character in characters)
                {
                    var pdf = pdfSupport.GeneratePdf(character);
                    if (pdf is not null)
                        memberPdfs.Add((character, pdf));
                }

                if (memberPdfs.Count > 0)
                {
                    var zipBytes = PartyZipBuilder.CreatePartyZip(memberPdfs);
                    if (zipBytes.Length > 0)
                    {
                        var zipFileName = PartyZipBuilder.GeneratePartyZipFileName(result.PartyName);
                        zipStream = new MemoryStream(zipBytes);
                        zipAttachment = new FileAttachment(zipStream, zipFileName);
                    }
                }
            }

            var partyEmbed = result.PartyCard;

            try
            {
                var isDm = IsDmInteraction(command.GuildId);
                IMessageChannel targetChannel = isDm
                    ? command.Channel
                    : await command.User.CreateDMChannelAsync();

                if (zipAttachment is not null)
                {
                    await targetChannel.SendFileAsync(zipAttachment.Value, embed: partyEmbed);
                }
                else
                {
                    await targetChannel.SendMessageAsync(embed: partyEmbed);
                }

                await command.FollowupAsync(text: GetPartyFollowupText(isDm), ephemeral: true);
            }
            catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                _logger.LogWarning(httpEx, "Cannot DM user {UserId}", command.User.Id);
                await command.FollowupAsync(
                    text: "I couldn't send you a DM. Please enable DMs from server members and try again.",
                    ephemeral: true);
            }
            finally
            {
                zipStream?.Dispose();
            }
        }
        catch (Exception sendEx)
        {
            _logger.LogError(sendEx, "Failed to send party cards");
            await command.FollowupAsync(
                embed: ResponseCardBuilder.Build("Party Send Failed",
                    "Something went wrong sending your party. Please try again.", new Color(200, 50, 50)),
                ephemeral: true);
        }
    }
}
