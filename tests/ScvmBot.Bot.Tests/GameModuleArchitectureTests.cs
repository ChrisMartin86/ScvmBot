using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Architecture tests that verify the module-based integration model:
/// - The bot can host modules without concrete MÖRK BORG startup wiring
/// - A module can register commands and renderers through the shared contract
/// - Startup fails clearly when a module is invalid
/// - Adding a second fake test module does not require changes to generic bot orchestration
/// </summary>
public class GameModuleArchitectureTests
{
    // ── Bot can host modules without concrete MÖRK BORG startup wiring ───────

    [Fact]
    public void GenerateCommandHandler_WorksWithAnyGameModule_NoConcreteMorkBorgWiring()
    {
        var fakeModule = new FakeModule("Fake Game", "fakegame");
        var registry = new RendererRegistry(new IResultRenderer[] { new FakeEmbedRenderer() });
        var handler = new GenerateCommandHandler(
            new IGameModule[] { fakeModule },
            registry,
            new GenerationDeliveryService(NullLogger<GenerationDeliveryService>.Instance),
            NullLogger<GenerateCommandHandler>.Instance);

        var command = handler.BuildCommand();

        Assert.Equal("generate", command.Name);
        Assert.Single(command.Options);
        Assert.Equal("fakegame", command.Options.First().Name);
    }

    // ── Module can register commands and renderers through the shared contract ─

    [Fact]
    public void Module_RegistersCommandAndRenderers_ThroughSharedContract()
    {
        var services = new ServiceCollection();
        var fakeModule = new FakeModule("Test", "test");
        services.AddSingleton<IGameModule>(fakeModule);
        services.AddSingleton<IResultRenderer>(new FakeEmbedRenderer());
        services.AddSingleton<RendererRegistry>();

        using var provider = services.BuildServiceProvider();
        var modules = provider.GetServices<IGameModule>().ToList();
        var registry = provider.GetRequiredService<RendererRegistry>();
        var renderer = registry.FindRenderer(
            new GenerationBatch<FakeCharacter>(new[] { new FakeCharacter { Name = "X" } }),
            OutputFormat.Card);

        Assert.Single(modules);
        Assert.Equal("Test", modules[0].Name);
        Assert.NotNull(renderer);
    }

    // ── Startup fails clearly when a module is invalid ───────────────────────

    [Fact]
    public void RendererRegistry_Throws_WhenTwoDifferentRenderers_ClaimSameResultTypeAndFormat()
    {
        var renderer1 = new FakeEmbedRenderer();
        var renderer2 = new AlternateFakeEmbedRenderer();

        var ex = Assert.Throws<InvalidOperationException>(
            () => new RendererRegistry(new IResultRenderer[] { renderer1, renderer2 }));

        Assert.Contains("Ambiguous renderer registration", ex.Message);
        Assert.Contains(nameof(FakeEmbedRenderer), ex.Message);
        Assert.Contains(nameof(AlternateFakeEmbedRenderer), ex.Message);
    }

    [Fact]
    public async Task MorkBorgModuleRegistration_FailsFast_WhenDataDirectoryMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Modules:MorkBorg:DataPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            })
            .Build();

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => new MorkBorgModuleRegistration().InitializeAsync(config));

        Assert.Contains("classes.json", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Adding a second module does not require changes to bot orchestration ──

    [Fact]
    public async Task TwoModules_CoexistWithoutOrchestrationChanges()
    {
        var module1 = new FakeModule("Alpha", "alpha");
        var module2 = new FakeModule("Beta", "beta");
        var registry = new RendererRegistry(new IResultRenderer[] { new FakeEmbedRenderer() });
        var handler = new GenerateCommandHandler(
            new IGameModule[] { module1, module2 },
            registry,
            new GenerationDeliveryService(NullLogger<GenerationDeliveryService>.Instance),
            NullLogger<GenerateCommandHandler>.Instance);

        var command = handler.BuildCommand();

        Assert.Equal(2, command.Options.Count);
        var optionNames = command.Options.Select(o => o.Name).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "alpha", "beta" }, optionNames);
    }

    [Fact]
    public async Task TwoModules_RoutesToCorrectModuleByCommandKey()
    {
        var module1 = new FakeModule("Alpha", "alpha");
        var module2 = new FakeModule("Beta", "beta");
        var registry = new RendererRegistry(new IResultRenderer[] { new FakeEmbedRenderer() });
        var handler = new GenerateCommandHandler(
            new IGameModule[] { module1, module2 },
            registry,
            new GenerationDeliveryService(NullLogger<GenerationDeliveryService>.Instance),
            NullLogger<GenerateCommandHandler>.Instance);

        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel,
            Options = new[]
            {
                new FakeOption
                {
                    Name = "beta",
                    Type = ApplicationCommandOptionType.SubCommandGroup,
                    Options = new IApplicationCommandInteractionDataOption[]
                    {
                        new FakeOption
                        {
                            Name = "character",
                            Type = ApplicationCommandOptionType.SubCommand,
                            Options = null
                        }
                    }
                }
            }
        };

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred);
        Assert.Equal(0, module1.InvocationCount);
        Assert.Equal(1, module2.InvocationCount);
    }

    [Fact]
    public async Task MorkBorgModuleRegistration_RegistersModuleAndRenderers()
    {
        // Verify the registration delegate correctly sets up services
        var services = new ServiceCollection();
        // Simulate a successful registration by providing a test data directory
        var dir = await TestDataBuilder.CreateMinimalDataDirectoryAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Modules:MorkBorg:DataPath"] = dir })
            .Build();
        var register = await new MorkBorgModuleRegistration().InitializeAsync(config);
        register(services);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        using var provider = services.BuildServiceProvider();
        var modules = provider.GetServices<IGameModule>().ToList();
        var renderers = provider.GetServices<IResultRenderer>().ToList();

        Assert.Single(modules);
        Assert.IsType<MorkBorgModule>(modules[0]);
        Assert.Equal(2, renderers.Count);
    }

    [Fact]
    public async Task MorkBorgModuleRegistration_ImplementsIModuleRegistration_AndRegistersServices()
    {
        var dir = await TestDataBuilder.CreateMinimalDataDirectoryAsync();

        IModuleRegistration registration = new MorkBorgModuleRegistration();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Modules:MorkBorg:DataPath"] = dir })
            .Build();
        var register = await registration.InitializeAsync(config);

        var services = new ServiceCollection();
        register(services);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        using var provider = services.BuildServiceProvider();
        var modules = provider.GetServices<IGameModule>().ToList();
        var renderers = provider.GetServices<IResultRenderer>().ToList();

        Assert.Single(modules);
        Assert.IsType<MorkBorgModule>(modules[0]);
        Assert.Equal(2, renderers.Count);
    }

    // ── Test doubles ─────────────────────────────────────────────────────────

    private sealed class FakeModule : IGameModule
    {
        public FakeModule(string name, string commandKey)
        {
            Name = name;
            CommandKey = commandKey;
        }

        public string Name { get; }
        public string CommandKey { get; }
        public int InvocationCount { get; private set; }

        public IReadOnlyList<SubCommandDefinition> SubCommands { get; } = new[]
        {
            new SubCommandDefinition("character", "Generate a character")
        };

        public Task<GenerateResult> HandleGenerateCommandAsync(
            string subCommand,
            IReadOnlyDictionary<string, object?> options,
            CancellationToken ct = default)
        {
            InvocationCount++;
            return Task.FromResult<GenerateResult>(
                new GenerationBatch<FakeCharacter>(
                    new[] { new FakeCharacter { Name = $"FakeChar-{CommandKey}" } }));
        }
    }

    private sealed class FakeEmbedRenderer : IResultRenderer
    {
        public Type ResultType => typeof(GenerationBatch<FakeCharacter>);

        public OutputFormat Format => OutputFormat.Card;

        public bool CanRender(GenerateResult result) => result is GenerationBatch<FakeCharacter>;

        public RenderOutput Render(GenerateResult result)
        {
            return new CardOutput(Title: "Fake", Description: "Fake character");
        }
    }

    /// <summary>
    /// A second renderer that claims the same (ResultType, Format) slot as FakeEmbedRenderer.
    /// Used to prove the registry rejects ambiguous registrations.
    /// </summary>
    private sealed class AlternateFakeEmbedRenderer : IResultRenderer
    {
        public Type ResultType => typeof(GenerationBatch<FakeCharacter>);

        public OutputFormat Format => OutputFormat.Card;

        public bool CanRender(GenerateResult result) => result is GenerationBatch<FakeCharacter>;

        public RenderOutput Render(GenerateResult result) =>
            new CardOutput(Title: "Alternate");
    }

    private class FakeOption : IApplicationCommandInteractionDataOption
    {
        public string Name { get; set; } = "";
        public ApplicationCommandOptionType Type { get; set; }
        public object? Value { get; set; }
        public IReadOnlyCollection<IApplicationCommandInteractionDataOption>? Options { get; set; }
    }
}
