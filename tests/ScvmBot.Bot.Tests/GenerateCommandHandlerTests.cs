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
        var embed = context.FollowupEmbeds[0];
        Assert.NotNull(embed);
        Assert.Equal("Error", embed!.Title);
        Assert.Contains("SubCommandGroup", embed.Description);
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
        var embed = context.FollowupEmbeds[0];
        Assert.NotNull(embed);
        Assert.Equal("Error", embed!.Title);
        Assert.Contains("unknown-game", embed.Description);
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

    // ── Constructor validation ─────────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsOnDuplicateModuleCommandKey()
    {
        var module1 = new StubModule("Game A", "samegame");
        var module2 = new StubModule("Game B", "samegame");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new GenerateCommandHandler(
                new ScvmBot.Modules.IGameModule[] { module1, module2 },
                CreateEmptyRegistry(),
                CreateDeliveryService(),
                NullLogger<GenerateCommandHandler>.Instance));

        Assert.Contains("Duplicate game module CommandKey", ex.Message);
        Assert.Contains("samegame", ex.Message);
    }

    // ── Zero-character party error ──────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SendsError_WhenPartyGeneratesZeroCharacters()
    {
        var emptyPartyModule = new EmptyPartyModule();
        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new MorkBorgPartyEmbedRenderer()
        });
        var handler = new GenerateCommandHandler(
            new ScvmBot.Modules.IGameModule[] { emptyPartyModule },
            registry,
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = new FakeMessageChannel(),
            Options = new[]
            {
                MakeSubCommandGroup("empty-party", MakeSubCommand("party"))
            }
        };

        await handler.HandleAsync(context);

        Assert.Single(context.FollowupEmbeds);
        var embed = context.FollowupEmbeds[0];
        Assert.NotNull(embed);
        Assert.Equal("Error", embed!.Title);
        Assert.Contains("no characters", embed.Description);
    }

    // ── DM privacy failure path ─────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ShowsDmPrivacyMessage_WhenDeliveryReturnsFalse()
    {
        var gs = await CreateMinimalGameSystemAsync();
        // FakeMessageChannel that simulates CannotSendMessageToUser
        var channel = new FakeMessageChannel
        {
            SendException = new Discord.Net.HttpException(
                System.Net.HttpStatusCode.Forbidden,
                request: null!,
                DiscordErrorCode.CannotSendMessageToUser)
        };
        var handler = new GenerateCommandHandler(
            new[] { gs },
            CreateMorkBorgRegistry(),
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel,
            Options = new[]
            {
                MakeSubCommandGroup("morkborg", MakeSubCommand("character"))
            }
        };

        await handler.HandleAsync(context);

        Assert.Contains(context.FollowupTexts, t => t != null && t.Contains("enable DMs"));
    }

    // ── Acknowledgement failure isolation ────────────────────────────────────

    [Fact]
    public async Task HandleAsync_DeliversResult_EvenWhenFollowupAcknowledgementFails()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var channel = new FakeMessageChannel();
        var handler = new GenerateCommandHandler(
            new[] { gs },
            CreateMorkBorgRegistry(),
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel,
            Options = new[]
            {
                MakeSubCommandGroup("morkborg", MakeSubCommand("character"))
            },
            // Inject a failure on the followup acknowledgement ("Here's your character!").
            // The delivery to channel should already have succeeded before this call,
            // so the handler should swallow the exception rather than re-throwing.
            FollowupException = new Discord.Net.HttpException(
                System.Net.HttpStatusCode.ServiceUnavailable, request: null!, null)
        };

        // Should not throw — acknowledgement failure is best-effort
        await handler.HandleAsync(context);

        // The result was delivered to the channel despite followup failure
        Assert.True(channel.SendMessageCallCount + channel.SendFileCallCount > 0,
            "Result should be delivered to channel even when followup acknowledgement fails");
        // No followup text was recorded because the exception was thrown
        Assert.Empty(context.FollowupTexts);
    }

    // ── File-rendering exception isolation ──────────────────────────────────

    [Fact]
    public async Task HandleAsync_DeliversCard_WhenTryRenderFileThrows()
    {
        var gs = await CreateMinimalGameSystemAsync();
        // Register both card and a throwing file renderer
        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new MorkBorgCharacterEmbedRenderer(),
            new ThrowingFileRenderer()
        });
        var channel = new FakeMessageChannel();
        var handler = new GenerateCommandHandler(
            new[] { gs },
            registry,
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel,
            Options = new[]
            {
                MakeSubCommandGroup("morkborg", MakeSubCommand("character"))
            }
        };

        await handler.HandleAsync(context);

        // Card was delivered without attachment
        Assert.Equal(1, channel.SendMessageCallCount);
        Assert.Equal(0, channel.SendFileCallCount);
        Assert.Contains("Here's your character!", context.FollowupTexts);
    }

    // ── Send failure path ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SendsErrorEmbed_WhenDeliveryThrowsUnexpectedException()
    {
        var gs = await CreateMinimalGameSystemAsync();
        var channel = new FakeMessageChannel
        {
            SendException = new Discord.Net.HttpException(
                System.Net.HttpStatusCode.InternalServerError,
                request: null!,
                (DiscordErrorCode)0)
        };
        var handler = new GenerateCommandHandler(
            new[] { gs },
            CreateMorkBorgRegistry(),
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = channel,
            Options = new[]
            {
                MakeSubCommandGroup("morkborg", MakeSubCommand("character"))
            }
        };

        await handler.HandleAsync(context);

        Assert.Contains(context.FollowupEmbeds, e => e?.Title == "Send Failed");
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

    private class StubModule : ScvmBot.Modules.IGameModule
    {
        public StubModule(string name, string commandKey) { Name = name; CommandKey = commandKey; }
        public string Name { get; }
        public string CommandKey { get; }
        public IReadOnlyList<SubCommandDefinition> SubCommands { get; } = new[]
        {
            new SubCommandDefinition("character", "Generate a character")
        };
        public Task<GenerateResult> HandleGenerateCommandAsync(
            string subCommand, IReadOnlyDictionary<string, object?> options, CancellationToken ct = default)
            => Task.FromResult<GenerateResult>(
                new CharacterGenerationResult<ScvmBot.Games.MorkBorg.Models.Character>(
                    new ScvmBot.Games.MorkBorg.Models.Character { Name = "Stub" }));
    }

    private class EmptyPartyModule : ScvmBot.Modules.IGameModule
    {
        public string Name => "Empty Party";
        public string CommandKey => "empty-party";
        public IReadOnlyList<SubCommandDefinition> SubCommands { get; } = new[]
        {
            new SubCommandDefinition("party", "Generate an empty party")
        };
        public Task<GenerateResult> HandleGenerateCommandAsync(
            string subCommand, IReadOnlyDictionary<string, object?> options, CancellationToken ct = default)
            => Task.FromResult<GenerateResult>(
                new PartyGenerationResult<ScvmBot.Games.MorkBorg.Models.Character>(
                    Characters: new List<ScvmBot.Games.MorkBorg.Models.Character>().AsReadOnly(),
                    PartyName: "Ghost Squad"));
    }

    private class ThrowingFileRenderer : IResultRenderer
    {
        public Type ResultType => typeof(CharacterGenerationResult<ScvmBot.Games.MorkBorg.Models.Character>);
        public OutputFormat Format => OutputFormat.File;
        public bool CanRender(GenerateResult result) => result is CharacterGenerationResult<ScvmBot.Games.MorkBorg.Models.Character>;
        public RenderOutput Render(GenerateResult result) =>
            throw new InvalidOperationException("PDF rendering deliberately failed");
    }
}
