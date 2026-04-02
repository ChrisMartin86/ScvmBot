using Discord;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;
using ScvmBot.Games.CyBorg.Generation;
using ScvmBot.Games.CyBorg.Models;
using ScvmBot.Games.CyBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.CyBorg;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Integration tests that initialize the Cy_Borg module with the real Data/ directory
/// content and verify that the full bot pipeline produces meaningful output.
/// Mirrors <see cref="RealDataIntegrationTests"/> for Cy_Borg.
/// </summary>
public class CyBorgRealDataIntegrationTests
{
    private static readonly string[] KnownClassNames =
    {
        "Nano-witch", "Street punk", "Wetware hacker",
        "Burned-out data courier", "Class-A android", "Drifter"
    };

    // ── 1. GenerateCommandHandler produces a real Cy_Borg character ──────────

    [Fact]
    public async Task HandleAsync_WithRealData_ProducesCharacterWithNameHpAndEquipment()
    {
        var (handler, channel, context) = await CreateSingleCharacterPipelineAsync(seed: 42);

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred);
        Assert.Equal(1, channel.SendMessageCallCount);

        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);

        // Non-empty name
        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Character card must have a non-empty name.");

        // Valid HP in description (format: "ClassName — HP N | Luck N | ¢N")
        Assert.NotNull(embed.Description);
        Assert.Matches(@"HP \d+", embed.Description);

        // Populated equipment field
        var equipmentField = Assert.Single(embed.Fields, f => f.Name == "Equipment");
        Assert.False(string.IsNullOrWhiteSpace(equipmentField.Value),
            "Equipment field must be populated when using real game data.");
    }

    // ── 2. Embed renderer produces real class names ───────────────────────────

    [Fact]
    public async Task EmbedRenderer_WithRealData_ContainsActualClassName()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(123));

        // Force a classed character
        var character = generator.Generate(new CyBorgCharacterGenerationOptions
        {
            ClassName = "Street punk"
        });

        var card = CyBorgCharacterEmbedRenderer.BuildCard(character);

        Assert.NotNull(card.Description);
        Assert.Contains("Street punk", card.Description);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(77)]
    [InlineData(999)]
    public async Task EmbedRenderer_WithRealData_AlwaysProducesPopulatedCard(int seed)
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(seed));
        var character = generator.Generate(new CyBorgCharacterGenerationOptions());

        var card = CyBorgCharacterEmbedRenderer.BuildCard(character);

        Assert.False(string.IsNullOrWhiteSpace(card.Title), "Card title (name) must not be blank.");
        Assert.Contains("HP", card.Description);
        Assert.NotNull(card.Fields);
        Assert.Contains(card.Fields!, f => f.Name == "Abilities");
        Assert.Contains(card.Fields!, f => f.Name == "Equipment");
    }

    // ── 3. Multi-character generation produces distinct characters ────────────

    [Fact]
    public async Task HandleAsync_WithRealData_MultiCharacterProducesDistinctCharacters()
    {
        var (handler, channel, context) = await CreateMultiCharacterPipelineAsync(count: 3, seed: 42);

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred);
        Assert.Equal(1, channel.SendMessageCallCount);

        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);

        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Roster card must have a non-empty group name.");

        Assert.NotNull(embed.Description);
        Assert.Contains("Characters", embed.Description);

        var memberLines = embed.Description!
            .Split('\n')
            .Where(l => l.TrimStart().StartsWith("•"))
            .Select(l => l.TrimStart('•', ' ').Trim())
            .ToList();

        Assert.Equal(3, memberLines.Count);
        Assert.Equal(memberLines.Count, memberLines.Distinct().Count());
    }

    [Fact]
    public async Task MultiCharacter_WithRealData_EachCharacterHasValidStats()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(55));
        var module = new CyBorgModule(generator, refData);

        var options = new Dictionary<string, object?> { ["count"] = (long)5 };
        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var batch = Assert.IsType<GenerationBatch<CyBorgCharacter>>(result);
        Assert.Equal(5, batch.Characters.Count);

        foreach (var character in batch.Characters)
        {
            Assert.False(string.IsNullOrWhiteSpace(character.Name),
                "Every character must have a name.");
            Assert.True(character.HitPoints >= 1,
                $"Character '{character.Name}' has HP {character.HitPoints}, expected >= 1.");
        }

        var distinctNames = batch.Characters.Select(c => c.Name).Distinct().Count();
        Assert.True(distinctNames > 1,
            "A batch of 5 characters should have more than 1 distinct name.");
    }

    // ── 4. Startup failure when required data is missing ─────────────────────

    [Fact]
    public async Task CyBorgReferenceDataService_MissingRequiredFile_FailsFastAtStartup()
    {
        var tempDir = SharedTestInfrastructure.CreateTempDirectory();
        try
        {
            // Write all required files except apps.json
            await File.WriteAllTextAsync(Path.Combine(tempDir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "gear.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "descriptions.json"), "{}", TestContext.Current.CancellationToken);
            // intentionally NOT writing apps.json

            var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
                CyBorgReferenceDataService.CreateAsync(tempDir));

            Assert.Contains("apps.json", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CyBorgReferenceDataService_MissingDescriptions_FailsFastAtStartup()
    {
        var tempDir = SharedTestInfrastructure.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "gear.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "apps.json"), "[]", TestContext.Current.CancellationToken);
            // intentionally NOT writing descriptions.json

            var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
                CyBorgReferenceDataService.CreateAsync(tempDir));

            Assert.Contains("descriptions.json", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // ── 5. CyBorg module discovered in the real bot pipeline ─────────────────

    [Fact]
    public async Task GenerateCommandHandler_DiscoversCyBorgModule_ByCommandKey()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(42));
        var module = new CyBorgModule(generator, refData);

        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new CyBorgCharacterEmbedRenderer()
        });
        var handler = new GenerateCommandHandler(
            new IGameModule[] { module },
            registry,
            new GenerationDeliveryService(NullLogger<GenerationDeliveryService>.Instance),
            NullLogger<GenerateCommandHandler>.Instance);

        var command = handler.BuildCommand();

        // The command should have a subcommand group for "cyborg"
        Assert.Equal("generate", command.Name);
        Assert.Single(command.Options);
        Assert.Equal("cyborg", command.Options.First().Name);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<CyBorgReferenceDataService> LoadRealReferenceDataAsync()
    {
        var dataPath = TestDataBuilder.GetRealCyBorgDataDirectoryPath();
        return await CyBorgReferenceDataService.CreateAsync(dataPath);
    }

    private static async Task<(GenerateCommandHandler handler, FakeMessageChannel channel, FakeCommandContext context)>
        CreateSingleCharacterPipelineAsync(int seed)
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(seed));
        var module = new CyBorgModule(generator, refData);

        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new CyBorgCharacterEmbedRenderer()
        });
        var handler = new GenerateCommandHandler(
            new IGameModule[] { module },
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
                MakeSubCommandGroup("cyborg", MakeSubCommand("character"))
            }
        };

        return (handler, channel, context);
    }

    private static async Task<(GenerateCommandHandler handler, FakeMessageChannel channel, FakeCommandContext context)>
        CreateMultiCharacterPipelineAsync(int count, int seed)
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CyBorgCharacterGeneratorFactory.Create(refData, new Random(seed));
        var module = new CyBorgModule(generator, refData);

        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new CyBorgCharacterEmbedRenderer()
        });
        var handler = new GenerateCommandHandler(
            new IGameModule[] { module },
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
                MakeSubCommandGroup("cyborg",
                    MakeSubCommand("character",
                        MakeOption("count", ApplicationCommandOptionType.Integer, (long)count)))
            }
        };

        return (handler, channel, context);
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
}
