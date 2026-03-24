using Discord;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Bot.Services;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Integration tests that initialize the MÖRK BORG module with the real Data/ directory
/// content (classes, weapons, armor, etc.) and verify that the full bot pipeline produces
/// meaningful output — not just that it survives empty data.
/// </summary>
public class RealDataIntegrationTests
{
    private static readonly string[] KnownClassNames =
    {
        "Esoteric Hermit", "Fanged Deserter", "Gutterborn Scum",
        "Heretical Priest", "Occult Herbmaster", "Wretched Royalty"
    };

    // ── 1. GenerateCommandHandler produces a real character ──────────────

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

        // Valid HP in description (format: "ClassName — HP N | Omens N | Ns")
        Assert.NotNull(embed.Description);
        Assert.Matches(@"HP \d+", embed.Description);

        // Populated equipment field
        var equipmentField = Assert.Single(embed.Fields, f => f.Name == "Equipment");
        Assert.False(string.IsNullOrWhiteSpace(equipmentField.Value),
            "Equipment field must be populated when using real game data.");
    }

    // ── 2. Embed renderer output contains real class names ──────────────

    [Fact]
    public async Task EmbedRenderer_WithRealData_ContainsActualClassName()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(123));
        var character = generator.Generate(new CharacterGenerationOptions());

        var card = MorkBorgCharacterEmbedRenderer.BuildCard(character);

        // The description line contains "ClassName — HP ..."
        // With real data the class name must be one of the known classes or "Classless"
        Assert.NotNull(card.Description);
        var validNames = KnownClassNames.Append("No Class").ToArray();
        Assert.True(
            validNames.Any(name => card.Description!.Contains(name)),
            $"Card description should contain a real class name. Got: {card.Description}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(77)]
    [InlineData(999)]
    public async Task EmbedRenderer_WithRealData_AlwaysProducesPopulatedCard(int seed)
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(seed));
        var character = generator.Generate(new CharacterGenerationOptions());

        var card = MorkBorgCharacterEmbedRenderer.BuildCard(character);

        Assert.False(string.IsNullOrWhiteSpace(card.Title), "Card title (name) must not be blank.");
        Assert.Contains("HP", card.Description);
        Assert.NotNull(card.Fields);
        Assert.Contains(card.Fields!, f => f.Name == "Abilities");
        Assert.Contains(card.Fields!, f => f.Name == "Equipment");
    }

    // ── 3. Multi-character generation produces distinct characters ───────

    [Fact]
    public async Task HandleAsync_WithRealData_MultiCharacterProducesDistinctCharacters()
    {
        var (handler, channel, context) = await CreateMultiCharacterPipelineAsync(count: 3, seed: 42);

        await handler.HandleAsync(context, TestContext.Current.CancellationToken);

        Assert.True(context.Deferred);
        Assert.Equal(1, channel.SendMessageCallCount);

        var embed = Assert.Single(channel.SentEmbeds);
        Assert.NotNull(embed);

        // Roster card title is a group name
        Assert.False(string.IsNullOrWhiteSpace(embed!.Title),
            "Roster card must have a non-empty group name.");

        // Description contains member list with bullet points
        Assert.NotNull(embed.Description);
        Assert.Contains("Characters", embed.Description);

        // Extract character names from bullet list ("• Name")
        var memberLines = embed.Description!
            .Split('\n')
            .Where(l => l.TrimStart().StartsWith("•"))
            .Select(l => l.TrimStart('•', ' ').Trim())
            .ToList();

        Assert.Equal(3, memberLines.Count);

        // Characters should have distinct names (different seeds internally)
        Assert.Equal(memberLines.Count, memberLines.Distinct().Count());
    }

    [Fact]
    public async Task MultiCharacter_WithRealData_EachCharacterHasValidStats()
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(55));
        var module = new MorkBorgModule(generator, refData);

        var options = new Dictionary<string, object?> { ["count"] = (long)5 };
        var result = await module.HandleGenerateCommandAsync("character", options, TestContext.Current.CancellationToken);

        var batch = Assert.IsType<GenerationBatch<Character>>(result);
        Assert.Equal(5, batch.Characters.Count);

        foreach (var character in batch.Characters)
        {
            Assert.False(string.IsNullOrWhiteSpace(character.Name),
                "Every character must have a name.");
            Assert.True(character.HitPoints >= 1,
                $"Character '{character.Name}' has HP {character.HitPoints}, expected >= 1.");
        }

        // At least some characters should differ (names drawn randomly)
        var distinctNames = batch.Characters.Select(c => c.Name).Distinct().Count();
        Assert.True(distinctNames > 1,
            "A batch of 5 characters should have more than 1 distinct name.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static async Task<MorkBorgReferenceDataService> LoadRealReferenceDataAsync()
    {
        var repoRoot = TestInfrastructure.GetRepositoryRoot();
        var dataPath = Path.Combine(repoRoot, "src", "ScvmBot.Games.MorkBorg", "Data");
        return await MorkBorgReferenceDataService.CreateAsync(dataPath);
    }

    private static async Task<(GenerateCommandHandler handler, FakeMessageChannel channel, FakeCommandContext context)>
        CreateSingleCharacterPipelineAsync(int seed)
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(seed));
        var module = new MorkBorgModule(generator, refData);

        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new MorkBorgCharacterEmbedRenderer()
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
                MakeSubCommandGroup("morkborg", MakeSubCommand("character"))
            }
        };

        return (handler, channel, context);
    }

    private static async Task<(GenerateCommandHandler handler, FakeMessageChannel channel, FakeCommandContext context)>
        CreateMultiCharacterPipelineAsync(int count, int seed)
    {
        var refData = await LoadRealReferenceDataAsync();
        var generator = CharacterGeneratorFactory.Create(refData, new Random(seed));
        var module = new MorkBorgModule(generator, refData);

        var registry = new RendererRegistry(new IResultRenderer[]
        {
            new MorkBorgCharacterEmbedRenderer()
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
                MakeSubCommandGroup("morkborg",
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
