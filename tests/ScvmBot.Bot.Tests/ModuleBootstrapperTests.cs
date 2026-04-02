using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    // ── Discovery finds both MorkBorg and CyBorg modules in the test dependency graph ─────

    [Fact]
    public async Task DiscoverAndInitialize_FindsMorkBorgModule_WhenDataPathConfigured()
    {
        var dataPath = Path.Combine(
            SharedTestInfrastructure.GetRepositoryRoot(),
            "src", "ScvmBot.Games.MorkBorg", "Data");
        var cyBorgDataPath = Path.Combine(
            SharedTestInfrastructure.GetRepositoryRoot(),
            "src", "ScvmBot.Games.CyBorg", "Data");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Modules:MorkBorg:DataPath"] = dataPath,
            ["Modules:CyBorg:DataPath"] = cyBorgDataPath
        });

        var registrations = await ModuleBootstrapper.DiscoverAndInitializeAsync(config);

        Assert.NotEmpty(registrations);

        // Verify the discovered registrations produce working modules
        var services = new ServiceCollection();
        foreach (var register in registrations)
            register(services);
        Assert.True(services.Count > 0);
    }

    // ── Discovery with minimal data directories still works ────────────────

    [Fact]
    public async Task DiscoverAndInitialize_WorksWithMinimalDataFiles()
    {
        var dir = await TestDataBuilder.CreateMinimalDataDirectoryAsync();
        var cyBorgDir = await TestDataBuilder.CreateMinimalCyBorgDataDirectoryAsync();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Modules:MorkBorg:DataPath"] = dir,
            ["Modules:CyBorg:DataPath"] = cyBorgDir
        });

        var registrations = await ModuleBootstrapper.DiscoverAndInitializeAsync(config);

        Assert.Equal(2, registrations.Count);

        // Verify the registrations can populate a DI container
        var services = new ServiceCollection();
        foreach (var register in registrations)
            register(services);
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

    // ── Configuration is passed to InitializeAsync ──────────────────────

    [Fact]
    public async Task DiscoverAndInitialize_AppliesConfiguredDataPath()
    {
        // Place data files in unique temp directories that the modules cannot
        // find without configuration. If config weren't passed to InitializeAsync,
        // the modules would fall back to default paths that don't contain
        // these files and would fail with FileNotFoundException.
        var dir = await TestDataBuilder.CreateMinimalDataDirectoryAsync();
        var cyBorgDir = await TestDataBuilder.CreateMinimalCyBorgDataDirectoryAsync();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Modules:MorkBorg:DataPath"] = dir,
            ["Modules:CyBorg:DataPath"] = cyBorgDir
        });

        var registrations = await ModuleBootstrapper.DiscoverAndInitializeAsync(config);

        Assert.Equal(2, registrations.Count);
        // If the wrong path were used, InitializeAsync would have thrown.
        // Verify the registrations produce services.
        var services = new ServiceCollection();
        foreach (var register in registrations)
            register(services);
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

        public Task<Action<IServiceCollection>> InitializeAsync(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger? logger = null)
            => Task.FromResult<Action<IServiceCollection>>(_ => { });
    }
}
