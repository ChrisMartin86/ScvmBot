using Discord;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class GenerateCommandHandlerTests
{
    // ── Error path: no subcommand group ──────────────────────────────────────

    [Fact]
    public async Task HandleAsync_DefersFirst_BeforeAnyFollowup()
    {
        var handler = CreateMinimalHandler();
        var context = new FakeCommandContext(); // no options

        await handler.HandleAsync(context);

        Assert.True(context.Deferred, "HandleAsync must call DeferAsync before doing anything else.");
    }

    [Fact]
    public async Task HandleAsync_SendsErrorEmbed_WhenNoSubcommandGroup()
    {
        var handler = CreateMinimalHandler();
        var context = new FakeCommandContext(); // no options

        await handler.HandleAsync(context);

        Assert.Single(context.FollowupEmbeds);
        Assert.NotNull(context.FollowupEmbeds[0]);
        Assert.Equal("Error", context.FollowupEmbeds[0]!.Title);
    }

    [Fact]
    public async Task HandleAsync_SendsErrorEmbed_WhenGameSystemUnknown()
    {
        var handler = CreateMinimalHandler(); // no game systems registered
        var context = new FakeCommandContext
        {
            Options = new[]
            {
                MakeSubCommandGroup("unknown-game", MakeSubCommand("character"))
            }
        };

        await handler.HandleAsync(context);

        Assert.Single(context.FollowupEmbeds);
        Assert.NotNull(context.FollowupEmbeds[0]);
        Assert.Equal("Error", context.FollowupEmbeds[0]!.Title);
    }

    // ── Success paths (DM context so handler uses context.Channel directly) ──

    [Fact]
    public async Task HandleAsync_SendsCharacterEmbed_ToChannel_WhenInDm()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var handler = new GenerateCommandHandler(
            new[] { gs },
            CreateMorkBorgRegistry(),
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null, // DM — uses context.Channel
            Channel = channel,
            Options = new[]
            {
                MakeSubCommandGroup("morkborg", MakeSubCommand("character"))
            }
        };

        await handler.HandleAsync(context);

        Assert.True(context.Deferred);
        Assert.True(channel.SendMessageCallCount + channel.SendFileCallCount > 0,
            "Expected at least one message or file to be sent to the channel.");
        Assert.Single(context.FollowupTexts, t => t is not null);
        Assert.Contains("Here's your character!", context.FollowupTexts);
    }

    [Fact]
    public async Task HandleAsync_SendsPartyEmbed_ToChannel_WhenInDm()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var handler = new GenerateCommandHandler(
            new[] { gs },
            CreateMorkBorgRegistry(),
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null, // DM
            Channel = channel,
            Options = new[]
            {
                MakeSubCommandGroup("morkborg",
                    MakeSubCommand("party"),
                    MakeOption("size", ApplicationCommandOptionType.Integer, (long)2))
            }
        };

        await handler.HandleAsync(context);

        Assert.True(context.Deferred);
        Assert.True(channel.SendMessageCallCount + channel.SendFileCallCount > 0,
            "Expected at least one message or file to be sent to the channel.");
        Assert.Contains("Here's your party!", context.FollowupTexts);
    }

    // ── Party PDF failure isolation ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_DeliversPartyCard_EvenWhenAllPdfRendersFail()
    {
        var gs = new ThrowingPdfPartyGameSystem();
        // Register only embed renderers — no PDF renderer for this game system
        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new MorkBorgPartyEmbedRenderer()
        });
        var handler = new GenerateCommandHandler(
            new ScvmBot.Modules.IGameModule[] { gs },
            registry,
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var channel = new FakeMessageChannel();
        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel,
            Options = new[]
            {
                MakeSubCommandGroup("throwing-pdf", MakeSubCommand("party"))
            }
        };

        await handler.HandleAsync(context);

        Assert.True(context.Deferred);
        Assert.Equal(1, channel.SendMessageCallCount);  // card sent without PDF archive
        Assert.Equal(0, channel.SendFileCallCount);
        Assert.Contains("Here's your party!", context.FollowupTexts);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GenerationDeliveryService CreateDeliveryService() =>
        new(NullLogger<GenerationDeliveryService>.Instance);

    private static RendererRegistry CreateEmptyRegistry() =>
        new(Array.Empty<IResultRenderer>());

    private static RendererRegistry CreateMorkBorgRegistry() =>
        new(new IResultRenderer[]
        {
            new MorkBorgCharacterEmbedRenderer(),
            new MorkBorgPartyEmbedRenderer()
        });

    private static GenerateCommandHandler CreateMinimalHandler() =>
        new(Array.Empty<ScvmBot.Modules.IGameModule>(), CreateEmptyRegistry(), CreateDeliveryService(), NullLogger<GenerateCommandHandler>.Instance);

    private static async Task<MorkBorgModule> CreateMinimalGameSystemAsync()
    {
        var dir = TestInfrastructure.CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
        var refData = await MorkBorgReferenceDataService.CreateAsync(dir);
        var generator = new CharacterGenerator(refData, new Random(42));
        return new MorkBorgModule(generator, refData);
    }

    private static IApplicationCommandInteractionDataOption MakeSubCommandGroup(
        string name,
        params IApplicationCommandInteractionDataOption[] subOptions) =>
        new FakeOption
        {
            Name = name,
            Type = ApplicationCommandOptionType.SubCommandGroup,
            Options = subOptions.ToList().AsReadOnly()
        };

    private static IApplicationCommandInteractionDataOption MakeSubCommand(string name) =>
        new FakeOption { Name = name, Type = ApplicationCommandOptionType.SubCommand, Options = null };

    private static IApplicationCommandInteractionDataOption MakeOption(
        string name,
        ApplicationCommandOptionType type,
        object? value) =>
        new FakeOption { Name = name, Type = type, Value = value, Options = null };

    private class FakeOption : IApplicationCommandInteractionDataOption
    {
        public string Name { get; set; } = "";
        public ApplicationCommandOptionType Type { get; set; }
        public object? Value { get; set; }
        public IReadOnlyCollection<IApplicationCommandInteractionDataOption>? Options { get; set; }
    }

    private class ThrowingPdfPartyGameSystem : IGameModule
    {
        public string Name => "Throwing PDF";
        public string CommandKey => "throwing-pdf";

        public IReadOnlyList<SubCommandDefinition> SubCommands { get; } = new[]
        {
            new SubCommandDefinition("party", "Generate a party")
        };

        public Task<GenerateResult> HandleGenerateCommandAsync(
            string subCommand,
            IReadOnlyDictionary<string, object?> options,
            CancellationToken ct = default)
        {
            var characters = new List<ScvmBot.Games.MorkBorg.Models.Character>
            {
                new ScvmBot.Games.MorkBorg.Models.Character { Name = "Skag" }
            };
            return Task.FromResult<GenerateResult>(
                new PartyGenerationResult<ScvmBot.Games.MorkBorg.Models.Character>(
                    Characters: characters.AsReadOnly(),
                    PartyName: "Test Party"));
        }
    }
}
