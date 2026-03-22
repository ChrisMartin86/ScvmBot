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
    private readonly CommandRegistrationOrchestrator _registrationOrchestrator;
    private readonly ILogger<BotService> _logger;

    public BotService(
        DiscordSocketClient client,
        IConfiguration configuration,
        IEnumerable<ISlashCommand> slashCommands,
        ILogger<BotService> logger)
    {
        _client = client;
        _configuration = configuration;

        var commands = new Dictionary<string, ISlashCommand>(StringComparer.OrdinalIgnoreCase);
        foreach (var cmd in slashCommands)
        {
            if (!commands.TryAdd(cmd.Name, cmd))
            {
                throw new InvalidOperationException(
                    $"Duplicate slash command name '{cmd.Name}': " +
                    $"'{commands[cmd.Name].GetType().Name}' and '{cmd.GetType().Name}' both register the same name. " +
                    $"Each ISlashCommand must have a unique Name.");
            }
        }
        _slashCommands = commands;

        _registrationOrchestrator = new CommandRegistrationOrchestrator(configuration, commands, logger);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var token = _configuration["Discord:Token"]
            ?? throw new InvalidOperationException("Discord token not found in configuration.");

        _client.Log += OnDiscordLog;
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += SlashCommandHandler;

        await _client.LoginAsync(TokenType.Bot, token);

        // Stay invisible until OnReadyAsync promotes status to Online.
        await _client.SetStatusAsync(UserStatus.Invisible);

        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Log -= OnDiscordLog;
        _client.Ready -= OnReadyAsync;
        _client.SlashCommandExecuted -= SlashCommandHandler;

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

    private async Task OnReadyAsync()
    {
        _logger.LogInformation("Connected as {BotUser}.", _client.CurrentUser);

        await _registrationOrchestrator.OnReadyAsync(
            registerGlobalAsync: props => _client.BulkOverwriteGlobalApplicationCommandsAsync(props),
            tryRegisterGuildAsync: async (guildId, props) =>
            {
                var guild = _client.GetGuild(guildId);
                if (guild is null)
                    return false;

                await guild.BulkOverwriteApplicationCommandAsync(props);
                _logger.LogInformation("Registered {CommandCount} slash command(s) to guild {GuildName} ({GuildId}).",
                    _slashCommands.Count, guild.Name, guild.Id);
                return true;
            });

        await _client.SetStatusAsync(UserStatus.Online);
        _logger.LogInformation("Bot is ready.");
    }

    private Task SlashCommandHandler(SocketSlashCommand command)
    {
        _logger.LogDebug("Received /{CommandName} from user {UserId} in {Context}.",
            command.Data.Name, command.User.Id,
            command.GuildId.HasValue ? $"guild {command.GuildId}" : "DM");

        if (_slashCommands.TryGetValue(command.Data.Name, out var handler))
        {
            // Fire and forget: handlers defer their response immediately, so the gateway task
            // can continue without waiting for the entire command processing to complete.
            // This prevents the "handler is blocking the gateway task" warning.
            var context = new SocketSlashCommandContext(command);
            _ = handler.HandleAsync(context).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "Unhandled exception in /{CommandName} handler.", command.Data.Name);
                }
            }, TaskScheduler.Default);

            return Task.CompletedTask;
        }
        else
        {
            _logger.LogWarning("No handler registered for /{CommandName}.", command.Data.Name);
            return command.RespondAsync("Unknown command.");
        }
    }
}
