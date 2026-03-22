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
    public void ResolveStrategy_NoGuildIds_ReturnsGlobal()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Bot:SyncCommands"] = "true"
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Global, strategy.Mode);
        Assert.Empty(strategy.GuildIds);
    }

    [Fact]
    public void ResolveStrategy_EmptyGuildIdsSection_ReturnsGlobal()
    {
        // An empty section with no children should resolve as global.
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds"] = null
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Global, strategy.Mode);
        Assert.Empty(strategy.GuildIds);
    }

    [Fact]
    public void ResolveStrategy_EmptyJsonArray_ReturnsGlobal()
    {
        // An empty JSON array [] produces Value="" with no children in .NET config.
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds"] = ""
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Global, strategy.Mode);
        Assert.Empty(strategy.GuildIds);
    }

    [Fact]
    public void ResolveStrategy_GuildIdsAllZero_Throws()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds:0"] = "0",
            ["Discord:GuildIds:1"] = "0"
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => CommandRegistrar.ResolveStrategy(config));

        Assert.Contains("invalid entry", ex.Message);
    }

    [Fact]
    public void ResolveStrategy_SingleValidGuildId_ReturnsGuildWithOneId()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds:0"] = "1296851784899366944"
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Guild, strategy.Mode);
        Assert.Single(strategy.GuildIds);
        Assert.Equal(1296851784899366944UL, strategy.GuildIds[0]);
    }

    [Fact]
    public void ResolveStrategy_MultipleValidGuildIds_ReturnsGuildWithAllIds()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds:0"] = "1296851784899366944",
            ["Discord:GuildIds:1"] = "123456789012345678",
            ["Discord:GuildIds:2"] = "987654321098765432"
        });

        var strategy = CommandRegistrar.ResolveStrategy(config);

        Assert.Equal(RegistrationMode.Guild, strategy.Mode);
        Assert.Equal(3, strategy.GuildIds.Count);
        Assert.Contains(1296851784899366944UL, strategy.GuildIds);
        Assert.Contains(123456789012345678UL, strategy.GuildIds);
        Assert.Contains(987654321098765432UL, strategy.GuildIds);
    }

    [Fact]
    public void ResolveStrategy_MixOfValidAndInvalidGuildIds_Throws()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds:0"] = "1296851784899366944",
            ["Discord:GuildIds:1"] = "0",
            ["Discord:GuildIds:2"] = "not-a-number",
            ["Discord:GuildIds:3"] = "123456789012345678"
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => CommandRegistrar.ResolveStrategy(config));

        Assert.Contains("invalid entry", ex.Message);
    }

    [Fact]
    public void ResolveStrategy_GuildIdsSectionWithOnlyInvalidEntries_Throws()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds:0"] = "not-a-number",
            ["Discord:GuildIds:1"] = ""
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => CommandRegistrar.ResolveStrategy(config));

        Assert.Contains("invalid entry", ex.Message);
    }

    [Fact]
    public void ResolveStrategy_CommaSeparatedStringValue_Throws()
    {
        // The canonical format is array-style keys: Discord:GuildIds:0, Discord:GuildIds:1, ...
        // A plain comma-separated string under Discord:GuildIds is a config mistake.
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Discord:GuildIds"] = "1296851784899366944,123456789012345678"
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => CommandRegistrar.ResolveStrategy(config));

        Assert.Contains("must be configured as an array", ex.Message);
    }
}
