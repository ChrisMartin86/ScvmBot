using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ScvmBot.Bot.Services;

/// <summary>
/// Encapsulates the decision logic for slash command registration:
/// whether to skip registration, global vs guild mode, zero-guild failure, etc.
/// Extracted from <see cref="BotService"/> to enable unit testing without a real
/// Discord connection.
/// </summary>
internal sealed class CommandRegistrationOrchestrator
{
    private readonly IConfiguration _configuration;
    private readonly IReadOnlyDictionary<string, ISlashCommand> _slashCommands;
    private readonly ILogger _logger;
    private bool _commandsRegistered;

    public CommandRegistrationOrchestrator(
        IConfiguration configuration,
        IReadOnlyDictionary<string, ISlashCommand> slashCommands,
        ILogger logger)
    {
        _configuration = configuration;
        _slashCommands = slashCommands;
        _logger = logger;
    }

    /// <summary>
    /// Called on each Ready event from the Discord gateway. Gates registration
    /// so it runs only once per process lifetime.
    /// </summary>
    /// <returns>True if registration was attempted on this call, false if skipped (reconnect).</returns>
    public async Task<bool> OnReadyAsync(
        Func<ApplicationCommandProperties[], Task> registerGlobalAsync,
        Func<ulong, ApplicationCommandProperties[], Task<bool>> tryRegisterGuildAsync)
    {
        if (_commandsRegistered)
        {
            _logger.LogInformation("Reconnected. Skipping command registration (already completed this session).");
            return false;
        }

        await RegisterCommandsAsync(registerGlobalAsync, tryRegisterGuildAsync);
        _commandsRegistered = true;
        return true;
    }

    internal async Task RegisterCommandsAsync(
        Func<ApplicationCommandProperties[], Task> registerGlobalAsync,
        Func<ulong, ApplicationCommandProperties[], Task<bool>> tryRegisterGuildAsync)
    {
        var syncCommands = _configuration.GetValue<bool>("Bot:SyncCommands");
        if (!syncCommands)
        {
            _logger.LogInformation("Skipping command registration (Bot:SyncCommands is not enabled).");
            return;
        }

        _logger.LogInformation("Bot:SyncCommands is enabled. Registering {CommandCount} slash command(s)...",
            _slashCommands.Count);

        var commandProperties = _slashCommands.Values
            .Select(cmd => cmd.BuildCommand().Build())
            .ToArray();

        var strategy = CommandRegistrar.ResolveStrategy(_configuration);
        if (strategy.Mode == RegistrationMode.Guild)
        {
            var successCount = 0;
            foreach (var guildId in strategy.GuildIds)
            {
                var registered = await tryRegisterGuildAsync(guildId, commandProperties);
                if (registered)
                    successCount++;
                else
                    _logger.LogWarning("Discord:GuildIds contains {GuildId} but the guild was not found. Skipping.", guildId);
            }

            if (successCount == 0)
            {
                throw new InvalidOperationException(
                    $"Guild-mode command registration failed: none of the {strategy.GuildIds.Count} " +
                    $"configured guild ID(s) could be resolved. " +
                    $"Verify Discord:GuildIds contains IDs of guilds this bot has joined.");
            }
        }
        else
        {
            await registerGlobalAsync(commandProperties);
            _logger.LogInformation("Registered {CommandCount} slash command(s) globally.", _slashCommands.Count);
        }

        foreach (var cmd in _slashCommands.Values)
            _logger.LogDebug("  Registered /{CommandName}", cmd.Name);
    }
}
