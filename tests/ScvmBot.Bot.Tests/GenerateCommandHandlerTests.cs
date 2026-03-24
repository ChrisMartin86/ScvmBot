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

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred, "HandleAsync must call DeferAsync before doing anything else.");
    }

    [Fact]
    public async Task HandleAsync_SendsErrorEmbed_WhenNoSubcommandGroup()
    {
        var handler = CreateMinimalHandler();
        var context = new FakeCommandContext(); // no options

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

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

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.Single(context.FollowupEmbeds);
        var embed = context.FollowupEmbeds[0];
        Assert.NotNull(embed);
        Assert.Equal("Error", embed!.Title);
        Assert.Contains("unknown-game", embed.Description);
    }

    // ── Command shape ─────────────────────────────────────────────────────

    [Fact]
    public async Task BuildCommand_CountOption_HasMaxValue()
    {
        var module = await CreateMinimalGameSystemAsync();
        var handler = new GenerateCommandHandler(
            new IGameModule[] { module },
            CreateEmptyRegistry(),
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);
        var command = handler.BuildCommand();

        var morkborg = command.Options.First(o => o.Name == "morkborg");
        var character = morkborg.Options.First(o => o.Name == "character");
        var countOpt = character.Options.First(o => o.Name == "count");

        Assert.Equal(GenerateCommandHandler.MaxDiscordCharacterCount, countOpt.MaxValue);
        Assert.Equal(1, countOpt.MinValue);
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

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred);
        Assert.Equal(1, channel.SendMessageCallCount);
        Assert.Contains("Here's your character!", context.FollowupTexts);

        // Verify the rendered card is a real character card, not a blank/error
        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);
        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Character card must have a non-empty title (character name).");
        Assert.NotNull(embed.Description);
        Assert.Contains("HP", embed.Description);
        Assert.Contains(embed.Fields, f => f.Name == "Abilities");
        Assert.Contains(embed.Fields, f => f.Name == "Equipment");
    }

    [Fact]
    public async Task HandleAsync_SendsMultiCharacterEmbed_ToChannel_WhenInDm()
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
                    MakeSubCommand("character",
                        MakeOption("count", ApplicationCommandOptionType.Integer, (long)2)))
            }
        };

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred);
        Assert.Equal(1, channel.SendMessageCallCount);
        Assert.Contains("Here are your characters!", context.FollowupTexts);

        // Verify the rendered card is a roster card with member list
        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);
        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Roster card must have a non-empty title (group name).");
        Assert.NotNull(embed.Description);
        Assert.Contains("Characters", embed.Description);
    }

    [Fact]
    public async Task HandleAsync_DeliversRosterCard_EvenWhenAllPdfRendersFail()
    {
        var gs = new MultiCharacterNoPdfGameSystem();
        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new MorkBorgCharacterEmbedRenderer()
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
                MakeSubCommandGroup("multi-char", MakeSubCommand("character"))
            }
        };

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred);
        Assert.Equal(1, channel.SendMessageCallCount);  // card sent without PDF archive
        Assert.Equal(0, channel.SendFileCallCount);
        Assert.Contains("Here are your characters!", context.FollowupTexts);

        // Verify the roster card has real content
        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);
        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Roster card must have a non-empty title.");
        Assert.NotNull(embed.Description);
        Assert.Contains("Characters", embed.Description);
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

    // ── Empty generation error ─────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SendsError_WhenModuleThrowsDueToEmptyGeneration()
    {
        var emptyModule = new EmptyResultModule();
        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new MorkBorgCharacterEmbedRenderer()
        });
        var handler = new GenerateCommandHandler(
            new ScvmBot.Modules.IGameModule[] { emptyModule },
            registry,
            CreateDeliveryService(),
            NullLogger<GenerateCommandHandler>.Instance);

        var context = new FakeCommandContext
        {
            GuildId = null,
            Channel = new FakeMessageChannel(),
            Options = new[]
            {
                MakeSubCommandGroup("empty-result", MakeSubCommand("character"))
            }
        };

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.Single(context.FollowupEmbeds);
        var embed = context.FollowupEmbeds[0];
        Assert.NotNull(embed);
        Assert.Equal("Error", embed!.Title);
        Assert.Contains("at least one character", embed.Description);
    }

    [Fact]
    public void GenerationBatch_RejectsEmptyCharacterList()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new GenerationBatch<ScvmBot.Games.MorkBorg.Models.Character>(
                new List<ScvmBot.Games.MorkBorg.Models.Character>().AsReadOnly()));

        Assert.Contains("at least one character", ex.Message);
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

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

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
        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        // The result was delivered to the channel despite followup failure
        Assert.Equal(1, channel.SendMessageCallCount);
        // No followup text was recorded because the exception was thrown
        Assert.Empty(context.FollowupTexts);

        // Verify the delivered card has real content
        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);
        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Character card must be delivered even when followup acknowledgement fails.");
        Assert.Contains(embed.Fields, f => f.Name == "Abilities");
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

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        // Card was delivered without attachment
        Assert.Equal(1, channel.SendMessageCallCount);
        Assert.Equal(0, channel.SendFileCallCount);
        Assert.Contains("Here's your character!", context.FollowupTexts);

        // Verify the rendered card is a real character card, not blank
        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);
        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Character card must have a title even when file rendering fails.");
        Assert.Contains(embed.Fields, f => f.Name == "Abilities");
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

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

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
            new MorkBorgCharacterEmbedRenderer()
        });

    private static GenerateCommandHandler CreateMinimalHandler() =>
        new(Array.Empty<ScvmBot.Modules.IGameModule>(), CreateEmptyRegistry(), CreateDeliveryService(), NullLogger<GenerateCommandHandler>.Instance);

    private static async Task<MorkBorgModule> CreateMinimalGameSystemAsync()
    {
        var dir = await TestDataBuilder.CreateMinimalDataDirectoryAsync();
        var refData = await MorkBorgReferenceDataService.CreateAsync(dir);
        var generator = CharacterGeneratorFactory.Create(refData, new Random(42));
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

    private static IApplicationCommandInteractionDataOption MakeSubCommand(
        string name,
        params IApplicationCommandInteractionDataOption[] childOptions) =>
        new FakeOption
        {
            Name = name,
            Type = ApplicationCommandOptionType.SubCommand,
            Options = childOptions.Length > 0 ? childOptions.ToList().AsReadOnly() : null
        };

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

    private class MultiCharacterNoPdfGameSystem : IGameModule
    {
        public string Name => "Multi Char";
        public string CommandKey => "multi-char";

        public IReadOnlyList<SubCommandDefinition> SubCommands { get; } = new[]
        {
            new SubCommandDefinition("character", "Generate characters")
        };

        public Task<GenerateResult> HandleGenerateCommandAsync(
            string subCommand,
            IReadOnlyDictionary<string, object?> options,
            CancellationToken ct = default)
        {
            var characters = new List<ScvmBot.Games.MorkBorg.Models.Character>
            {
                new ScvmBot.Games.MorkBorg.Models.Character { Name = "Skag" },
                new ScvmBot.Games.MorkBorg.Models.Character { Name = "Bleth" }
            };
            return Task.FromResult<GenerateResult>(
                new GenerationBatch<ScvmBot.Games.MorkBorg.Models.Character>(
                    Characters: characters.AsReadOnly(),
                    GroupName: "Test Group"));
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
                new GenerationBatch<ScvmBot.Games.MorkBorg.Models.Character>(
                    new[] { new ScvmBot.Games.MorkBorg.Models.Character { Name = "Stub" } }));
    }

    private class EmptyResultModule : ScvmBot.Modules.IGameModule
    {
        public string Name => "Empty Result";
        public string CommandKey => "empty-result";
        public IReadOnlyList<SubCommandDefinition> SubCommands { get; } = new[]
        {
            new SubCommandDefinition("character", "Generate nothing")
        };
        public Task<GenerateResult> HandleGenerateCommandAsync(
            string subCommand, IReadOnlyDictionary<string, object?> options, CancellationToken ct = default)
            => Task.FromResult<GenerateResult>(
                new GenerationBatch<ScvmBot.Games.MorkBorg.Models.Character>(
                    new List<ScvmBot.Games.MorkBorg.Models.Character>().AsReadOnly()));
    }

    private class ThrowingFileRenderer : IResultRenderer
    {
        public Type ResultType => typeof(GenerationBatch<ScvmBot.Games.MorkBorg.Models.Character>);
        public OutputFormat Format => OutputFormat.File;
        public bool CanRender(GenerateResult result) => result is GenerationBatch<ScvmBot.Games.MorkBorg.Models.Character>;
        public RenderOutput Render(GenerateResult result) =>
            throw new InvalidOperationException("PDF rendering deliberately failed");
    }
}
