using Microsoft.Extensions.Configuration;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Tests for <see cref="ModuleBootstrapper"/> discovery and initialization contracts.
/// These tests exercise the bootstrapper within the test assembly's dependency context,
/// which includes ScvmBot.Modules.MorkBorg in its dependency graph.
/// </summary>
public class ModuleBootstrapperTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    // ── Discovery finds MorkBorg module in the test dependency graph ─────

    [Fact]
    public async Task DiscoverAndInitialize_FindsMorkBorgModule_WhenDataPathConfigured()
    {
        var dataPath = Path.Combine(
            SharedTestInfrastructure.GetRepositoryRoot(),
            "src", "ScvmBot.Games.MorkBorg", "Data");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Modules:MorkBorg:DataPath"] = dataPath
        });

        var modules = await ModuleBootstrapper.DiscoverAndInitializeAsync(config);

        Assert.NotEmpty(modules);
        Assert.Contains(modules, m => m is MorkBorgModuleRegistration);
    }

    // ── Discovery with minimal data directory still works ────────────────

    [Fact]
    public async Task DiscoverAndInitialize_WorksWithMinimalDataFiles()
    {
        var dir = SharedTestInfrastructure.CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Modules:MorkBorg:DataPath"] = dir
        });

        var modules = await ModuleBootstrapper.DiscoverAndInitializeAsync(config);

        Assert.Single(modules);
        var registration = Assert.IsType<MorkBorgModuleRegistration>(modules[0]);

        // Verify the registration can populate a DI container
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        registration.Register(services);
        Assert.True(services.Count > 0);
    }

    // ── Discovery propagates initialization failure ──────────────────────

    [Fact]
    public async Task DiscoverAndInitialize_PropagatesInitializationFailure_WhenDataMissing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Modules:MorkBorg:DataPath"] = missingPath
        });

        // MorkBorgModuleRegistration.InitializeAsync() should throw FileNotFoundException
        // when the data directory doesn't contain required files
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => ModuleBootstrapper.DiscoverAndInitializeAsync(config));
    }

    // ── Configure is called before InitializeAsync ──────────────────────

    [Fact]
    public async Task DiscoverAndInitialize_PassesConfigurationToModule()
    {
        // If Configure is not called, the module would use default paths.
        // By providing a custom data path via config, we prove Configure is invoked.
        var dir = SharedTestInfrastructure.CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Modules:MorkBorg:DataPath"] = dir
        });

        // If Configure weren't called, InitializeAsync would try the default
        // data path which may or may not exist — using a temp dir proves
        // the config path was applied.
        var modules = await ModuleBootstrapper.DiscoverAndInitializeAsync(config);

        Assert.Single(modules);
    }
}
