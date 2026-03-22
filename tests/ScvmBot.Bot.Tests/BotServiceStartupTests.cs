using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Tests for BotService constructor validation and
/// <see cref="CommandRegistrationOrchestrator"/> runtime registration behavior.
/// </summary>
public class BotServiceStartupTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection().Build();

    private static DiscordSocketClient CreateClient() => new();

    // ── Constructor: duplicate command-name detection ────────────────────

    [Fact]
    public void Constructor_ThrowsOnDuplicateSlashCommandName()
    {
        var cmd1 = new FakeSlashCommand("generate");
        var cmd2 = new FakeSlashCommand("generate");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new BotService(CreateClient(), EmptyConfig(), new ISlashCommand[] { cmd1, cmd2 },
                NullLogger<BotService>.Instance));

        Assert.Contains("Duplicate slash command name", ex.Message);
        Assert.Contains("generate", ex.Message);
    }

    [Fact]
    public void Constructor_AcceptsDistinctCommandNames()
    {
        var cmd1 = new FakeSlashCommand("hello");
        var cmd2 = new FakeSlashCommand("generate");

        var service = new BotService(CreateClient(), EmptyConfig(),
            new ISlashCommand[] { cmd1, cmd2 },
            NullLogger<BotService>.Instance);

        Assert.NotNull(service);
    }

    // ── SyncCommands = false: skips registration entirely ────────────────

    [Fact]
    public async Task RegisterCommands_SkipsRegistration_WhenSyncCommandsIsFalse()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Bot:SyncCommands"] = "false"
        });
        var commands = BuildCommandMap(new FakeSlashCommand("hello"));
        var orchestrator = new CommandRegistrationOrchestrator(
            config, commands, NullLogger<BotService>.Instance);

        var globalRegistered = false;
        await orchestrator.RegisterCommandsAsync(
            registerGlobalAsync: _ => { globalRegistered = true; return Task.CompletedTask; },
            tryRegisterGuildAsync: (_, _) => Task.FromResult(true));

        Assert.False(globalRegistered, "No registration should occur when SyncCommands is false");
    }

    // ── Global mode: calls registerGlobalAsync ──────────────────────────

    [Fact]
    public async Task RegisterCommands_RegistersGlobally_WhenNoGuildIds()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Bot:SyncCommands"] = "true"
        });
        var commands = BuildCommandMap(new FakeSlashCommand("generate"));
        var orchestrator = new CommandRegistrationOrchestrator(
            config, commands, NullLogger<BotService>.Instance);

        var globalRegistered = false;
        await orchestrator.RegisterCommandsAsync(
            registerGlobalAsync: _ => { globalRegistered = true; return Task.CompletedTask; },
            tryRegisterGuildAsync: (_, _) => Task.FromResult(true));

        Assert.True(globalRegistered, "Should register globally when no guild IDs are configured");
    }

    // ── Guild mode: zero resolvable guilds throws ───────────────────────

    [Fact]
    public async Task RegisterCommands_Throws_WhenGuildModeAndZeroGuildsResolvable()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Bot:SyncCommands"] = "true",
            ["Discord:GuildIds:0"] = "111111111111111111"
        });
        var commands = BuildCommandMap(new FakeSlashCommand("generate"));
        var orchestrator = new CommandRegistrationOrchestrator(
            config, commands, NullLogger<BotService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.RegisterCommandsAsync(
                registerGlobalAsync: _ => Task.CompletedTask,
                tryRegisterGuildAsync: (_, _) => Task.FromResult(false))); // all guilds unresolvable

        Assert.Contains("Guild-mode command registration failed", ex.Message);
        Assert.Contains("could be resolved", ex.Message);
    }

    // ── Guild mode: at least one resolvable guild succeeds ──────────────

    [Fact]
    public async Task RegisterCommands_Succeeds_WhenAtLeastOneGuildResolvable()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Bot:SyncCommands"] = "true",
            ["Discord:GuildIds:0"] = "111111111111111111",
            ["Discord:GuildIds:1"] = "222222222222222222"
        });
        var commands = BuildCommandMap(new FakeSlashCommand("generate"));
        var orchestrator = new CommandRegistrationOrchestrator(
            config, commands, NullLogger<BotService>.Instance);

        var registeredGuildIds = new List<ulong>();
        await orchestrator.RegisterCommandsAsync(
            registerGlobalAsync: _ => Task.CompletedTask,
            tryRegisterGuildAsync: (guildId, _) =>
            {
                // Only the second guild is resolvable
                if (guildId == 222222222222222222UL)
                {
                    registeredGuildIds.Add(guildId);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            });

        Assert.Single(registeredGuildIds);
        Assert.Equal(222222222222222222UL, registeredGuildIds[0]);
    }

    // ── Reconnect: skips duplicate registration ─────────────────────────

    [Fact]
    public async Task OnReady_SkipsRegistration_OnSecondCall()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Bot:SyncCommands"] = "true"
        });
        var commands = BuildCommandMap(new FakeSlashCommand("generate"));
        var orchestrator = new CommandRegistrationOrchestrator(
            config, commands, NullLogger<BotService>.Instance);

        var registrationCount = 0;
        Func<Discord.ApplicationCommandProperties[], Task> registerGlobal =
            _ => { registrationCount++; return Task.CompletedTask; };
        Func<ulong, Discord.ApplicationCommandProperties[], Task<bool>> tryRegisterGuild =
            (_, _) => Task.FromResult(true);

        var firstResult = await orchestrator.OnReadyAsync(registerGlobal, tryRegisterGuild);
        var secondResult = await orchestrator.OnReadyAsync(registerGlobal, tryRegisterGuild);

        Assert.True(firstResult, "First OnReady should attempt registration");
        Assert.False(secondResult, "Second OnReady should skip registration (reconnect)");
        Assert.Equal(1, registrationCount);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static IReadOnlyDictionary<string, ISlashCommand> BuildCommandMap(
        params ISlashCommand[] commands)
    {
        var dict = new Dictionary<string, ISlashCommand>(StringComparer.OrdinalIgnoreCase);
        foreach (var cmd in commands)
            dict[cmd.Name] = cmd;
        return dict;
    }

    // ── Test doubles ─────────────────────────────────────────────────────

    private sealed class FakeSlashCommand : ISlashCommand
    {
        public FakeSlashCommand(string name) => Name = name;
        public string Name { get; }
        public Discord.SlashCommandBuilder BuildCommand() =>
            new Discord.SlashCommandBuilder().WithName(Name).WithDescription("test");
        public Task HandleAsync(ISlashCommandContext context) => Task.CompletedTask;
    }
}
