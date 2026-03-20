using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;
using ScvmBot.Rendering;
using ScvmBot.Rendering.MorkBorg;

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
            new CharacterGenerationResult(new FakeCharacter { Name = "X" }),
            OutputFormat.DiscordEmbed);

        Assert.Single(modules);
        Assert.Equal("Test", modules[0].Name);
        Assert.NotNull(renderer);
    }

    // ── Startup fails clearly when a module is invalid ───────────────────────

    [Fact]
    public async Task MorkBorgModuleRegistration_FailsFast_WhenDataDirectoryMissing()
    {
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => MorkBorgModuleRegistration.CreateAsync(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));

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

        await handler.HandleAsync(context);

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
        var dir = TestInfrastructure.CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

        var register = await MorkBorgModuleRegistration.CreateAsync(dir);
        register(services);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        using var provider = services.BuildServiceProvider();
        var modules = provider.GetServices<IGameModule>().ToList();
        var renderers = provider.GetServices<IResultRenderer>().ToList();

        Assert.Single(modules);
        Assert.IsType<MorkBorgModule>(modules[0]);
        Assert.Equal(4, renderers.Count);
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

        public SlashCommandOptionBuilder BuildCommandGroupOptions() =>
            new SlashCommandOptionBuilder()
                .WithName(CommandKey)
                .WithDescription($"{Name} game system")
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("character")
                    .WithDescription("Generate a character")
                    .WithType(ApplicationCommandOptionType.SubCommand));

        public Task<GenerateResult> HandleGenerateCommandAsync(
            IReadOnlyCollection<IApplicationCommandInteractionDataOption>? subCommandOptions,
            CancellationToken ct = default)
        {
            InvocationCount++;
            return Task.FromResult<GenerateResult>(
                new CharacterGenerationResult(
                    new FakeCharacter { Name = $"FakeChar-{CommandKey}" }));
        }
    }

    private sealed class FakeEmbedRenderer : IResultRenderer
    {
        public OutputFormat Format => OutputFormat.DiscordEmbed;

        public bool CanRender(GenerateResult result) => result is CharacterGenerationResult;

        public RenderOutput Render(GenerateResult result)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Fake")
                .WithDescription("Fake character")
                .Build();
            return new EmbedOutput(embed);
        }
    }

    private class FakeOption : IApplicationCommandInteractionDataOption
    {
        public string Name { get; set; } = "";
        public ApplicationCommandOptionType Type { get; set; }
        public object? Value { get; set; }
        public IReadOnlyCollection<IApplicationCommandInteractionDataOption>? Options { get; set; }
    }
}
