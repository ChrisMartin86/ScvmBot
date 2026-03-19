using Microsoft.Extensions.Configuration;
using ScvmBot.Bot.Services;

namespace ScvmBot.Bot.Tests;

public class CommandRegistrarTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void ResolveStrategy_NoGuildId_ReturnsGlobal()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Bot:SyncCommands"] = "true"
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Global, strategy.Mode);
        Assert.Null(strategy.GuildId);
    }

    [Fact]
    public void ResolveStrategy_EmptyGuildId_ReturnsGlobal()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildId"] = ""
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Global, strategy.Mode);
        Assert.Null(strategy.GuildId);
    }

    [Fact]
    public void ResolveStrategy_GuildIdIsZero_ReturnsGlobal()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildId"] = "0"
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Global, strategy.Mode);
        Assert.Null(strategy.GuildId);
    }

    [Fact]
    public void ResolveStrategy_ValidGuildId_ReturnsGuild()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildId"] = "1296851784899366944"
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Guild, strategy.Mode);
        Assert.Equal(1296851784899366944UL, strategy.GuildId);
    }

    [Fact]
    public void ResolveStrategy_GuildIdFromEnvVarStyle_ReturnsGuild()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildId"] = "123456789012345678"
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Guild, strategy.Mode);
        Assert.Equal(123456789012345678UL, strategy.GuildId);
    }

    [Fact]
    public void ResolveStrategy_GuildIdNull_ReturnsGlobal()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildId"] = null
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Global, strategy.Mode);
        Assert.Null(strategy.GuildId);
    }
}
