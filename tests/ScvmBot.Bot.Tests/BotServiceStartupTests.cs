using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Tests for BotService constructor validation and startup contracts.
/// BotService is marked ExcludeFromCodeCoverage because its socket lifecycle
/// requires a real Discord connection, but the constructor enforces important
/// invariants that are testable without a connection.
/// </summary>
public class BotServiceStartupTests
{
    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection().Build();

    private static DiscordSocketClient CreateClient() => new();

    // ── Duplicate slash command name detection ───────────────────────────

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

    [Fact]
    public void Constructor_AcceptsEmptyCommandList()
    {
        var service = new BotService(CreateClient(), EmptyConfig(),
            Array.Empty<ISlashCommand>(),
            NullLogger<BotService>.Instance);

        Assert.NotNull(service);
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
