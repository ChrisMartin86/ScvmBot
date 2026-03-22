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
    public async Task DiscoverAndInitialize_AppliesConfiguredDataPath()
    {
        // Place data files in a unique temp directory that the module cannot
        // find without Configure being called first. If Configure were skipped,
        // InitializeAsync would fall back to a default path that doesn't
        // contain these files and would fail with FileNotFoundException.
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
        // Verify the module was configured with our custom path by exercising
        // its Register method. If the wrong path were used, InitializeAsync
        // would have thrown before reaching here.
        var registration = Assert.IsType<MorkBorgModuleRegistration>(modules[0]);
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        registration.Register(services);
        Assert.True(services.Count > 0);
    }

    // ── Zero discovered modules throws ──────────────────────────────────

    [Fact]
    public async Task InitializeFromTypes_Throws_WhenNoModulesDiscovered()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ModuleBootstrapper.InitializeFromTypesAsync(
                Array.Empty<Type>(), config));

        Assert.Contains("No game modules were discovered", ex.Message);
    }

    // ── Missing parameterless constructor throws ────────────────────────

    [Fact]
    public async Task InitializeFromTypes_Throws_WhenModuleLacksParameterlessConstructor()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ModuleBootstrapper.InitializeFromTypesAsync(
                new[] { typeof(NoParameterlessCtorRegistration) }, config));

        Assert.Contains("does not have a public parameterless constructor", ex.Message);
        Assert.Contains(nameof(NoParameterlessCtorRegistration), ex.Message);
    }

    // ── Test doubles ────────────────────────────────────────────────────

    private class NoParameterlessCtorRegistration : IModuleRegistration
    {
        private readonly string _required;

        // No parameterless constructor — this should be rejected
        public NoParameterlessCtorRegistration(string required) => _required = required;

        public Task InitializeAsync() => Task.CompletedTask;
        public void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
}
