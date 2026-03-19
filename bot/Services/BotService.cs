using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace ScvmBot.Bot.Services;

[ExcludeFromCodeCoverage(Justification = "Discord socket lifecycle infrastructure; requires a real Discord connection to test.")]
public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly IReadOnlyDictionary<string, ISlashCommand> _slashCommands;
    private readonly ILogger<BotService> _logger;

    public BotService(
        DiscordSocketClient client,
        IConfiguration configuration,
        IEnumerable<ISlashCommand> slashCommands,
        ILogger<BotService> logger)
    {
        _client = client;
        _configuration = configuration;
        _slashCommands = slashCommands.ToDictionary(cmd => cmd.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var token = _configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord token not found in configuration.");

        _client.Log += OnDiscordLog;
        _client.Ready += ReadyAsync;
        _client.SlashCommandExecuted += SlashCommandHandler;

        await _client.LoginAsync(TokenType.Bot, token);

        // Stay invisible until ReadyAsync registers commands.
        await _client.SetStatusAsync(UserStatus.Invisible);

        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.SetStatusAsync(UserStatus.Invisible);
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    private Task OnDiscordLog(LogMessage log)
    {
        var level = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information,
        };

        _logger.Log(level, log.Exception, "[{Source}] {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation("Connected as {BotUser}.", _client.CurrentUser);

        var syncCommands = _configuration.GetValue<bool>("Bot:SyncCommands");
        if (syncCommands)
        {
            _logger.LogInformation("Bot:SyncCommands is enabled. Registering {CommandCount} slash command(s)...",
                _slashCommands.Count);

            try
            {
                var commandProperties = _slashCommands.Values
                    .Select(cmd => cmd.BuildCommand().Build())
                    .ToArray();

                await _client.BulkOverwriteGlobalApplicationCommandsAsync(commandProperties);

                foreach (var cmd in _slashCommands.Values)
                    _logger.LogDebug("  Registered /{CommandName}", cmd.Name);

                _logger.LogInformation("Registered {CommandCount} slash command(s) globally.", _slashCommands.Count);
            }
            catch (HttpException ex)
            {
                _logger.LogCritical(ex, "Failed to register slash commands. Bot will not be functional.");
                throw;
            }
        }
        else
        {
            _logger.LogInformation("Skipping command registration (Bot:SyncCommands is not enabled).");
        }

        await _client.SetStatusAsync(UserStatus.Online);
        _logger.LogInformation("Bot is ready.");
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        _logger.LogDebug("Received /{CommandName} from user {UserId} in {Context}.",
            command.Data.Name, command.User.Id,
            command.GuildId.HasValue ? $"guild {command.GuildId}" : "DM");

        if (_slashCommands.TryGetValue(command.Data.Name, out var handler))
        {
            await handler.HandleAsync(command);
        }
        else
        {
            _logger.LogWarning("No handler registered for /{CommandName}.", command.Data.Name);
            await command.RespondAsync("Unknown command.");
        }
    }
}
