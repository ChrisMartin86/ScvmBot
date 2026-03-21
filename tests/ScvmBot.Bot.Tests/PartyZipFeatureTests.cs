using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

// =====================================================================
// PartyNameGeneratorTests
// =====================================================================
public class PartyNameGeneratorTests
{
    [Fact]
    public void Generate_ReturnsSuppliedName_WhenProvided()
    {
        var characters = new List<Character>
        {
            new() { Name = "Svein" }
        };

        var result = PartyNameGenerator.Generate(characters, "The Doom Squad");

        Assert.Equal("The Doom Squad", result);
    }

    [Fact]
    public void Generate_CreatesRandomName_WhenNotProvided()
    {
        var characters = new List<Character>
        {
            new() { Name = "Karg" },
            new() { Name = "Bleth" }
        };

        var result = PartyNameGenerator.Generate(characters, null, new Random(42));

        Assert.False(string.IsNullOrWhiteSpace(result));
        // Should use one of the character names in a template pattern
        Assert.True(result.Contains("Karg") || result.Contains("Bleth"));
    }

    [Fact]
    public void Generate_UsesFirstCharacterName_InTemplate()
    {
        var characters = new List<Character>
        {
            new() { Name = "Solo" }
        };

        // rng.Next(1) always returns 0, selecting "Solo"
        var rng = new DeterministicRandom(new[] { 0, 0 });
        var result = PartyNameGenerator.Generate(characters, null, rng);

        Assert.Contains("Solo", result);
    }

    [Fact]
    public void Generate_GeneratesDefaultName_WhenNoCharacters()
    {
        var characters = new List<Character>();

        var result = PartyNameGenerator.Generate(characters, null, new Random(42));

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.StartsWith("The ", result);
    }
}

// =====================================================================
// PartyEmbedBuilderTests
// =====================================================================
public class PartyEmbedBuilderTests
{
    [Fact]
    public void Build_IncludesPartyName_AsTitle()
    {
        var members = CreateMembers(2);
        var card = MorkBorgPartyEmbedRenderer.BuildCard("The Doomed", members);

        Assert.Equal("The Doomed", card.Title);
    }

    [Fact]
    public void Build_IncludesPartySize_InDescription()
    {
        var members = CreateMembers(3);
        var card = MorkBorgPartyEmbedRenderer.BuildCard("Squad", members);

        Assert.Contains("Party of 3", card.Description);
    }

    [Fact]
    public void Build_ListsAllMembers_InDescription()
    {
        var members = new List<Character>
        {
            new Character { Name = "Alpha" },
            new Character { Name = "Beta" },
            new Character { Name = "Gamma" }
        };

        var card = MorkBorgPartyEmbedRenderer.BuildCard("Party", members);

        Assert.Contains("Alpha", card.Description);
        Assert.Contains("Beta", card.Description);
        Assert.Contains("Gamma", card.Description);
    }

    [Fact]
    public void Build_FormatsCorrectly_WithMultipleMembers()
    {
        var members = CreateMembers(5);
        var card = MorkBorgPartyEmbedRenderer.BuildCard("Big Party", members);

        // Description contains "Party of 5" plus one bullet line per member
        var lines = card.Description!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // First line is "Party of 5", remaining 5 are bullet items
        Assert.Equal(6, lines.Length);
        foreach (var line in lines.Skip(1))
            Assert.StartsWith("•", line.Trim());
    }

    [Fact]
    public void Build_HasNoFields()
    {
        var members = CreateMembers(3);
        var card = MorkBorgPartyEmbedRenderer.BuildCard("Party", members);

        Assert.Null(card.Fields);
    }

    private static List<Character> CreateMembers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Character { Name = $"Char{i}" })
            .ToList();
    }
}
