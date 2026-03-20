using Discord;
using ScvmBot.Bot.Rendering.MorkBorg;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Models;

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
        var embed = MorkBorgPartyEmbedRenderer.BuildEmbed("The Doomed", members);

        Assert.Equal("The Doomed", embed.Title);
    }

    [Fact]
    public void Build_IncludesPartySize_InDescription()
    {
        var members = CreateMembers(3);
        var embed = MorkBorgPartyEmbedRenderer.BuildEmbed("Squad", members);

        Assert.Contains("Party of 3", embed.Description);
    }

    [Fact]
    public void Build_ListsAllMembers_InDescription()
    {
        var members = new List<ICharacter>
        {
            new FakeCharacter { Name = "Alpha" },
            new FakeCharacter { Name = "Beta" },
            new FakeCharacter { Name = "Gamma" }
        };

        var embed = MorkBorgPartyEmbedRenderer.BuildEmbed("Party", members);

        Assert.Contains("Alpha", embed.Description);
        Assert.Contains("Beta", embed.Description);
        Assert.Contains("Gamma", embed.Description);
    }

    [Fact]
    public void Build_FormatsCorrectly_WithMultipleMembers()
    {
        var members = CreateMembers(5);
        var embed = MorkBorgPartyEmbedRenderer.BuildEmbed("Big Party", members);

        // Description contains "Party of 5" plus one bullet line per member
        var lines = embed.Description!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // First line is "Party of 5", remaining 5 are bullet items
        Assert.Equal(6, lines.Length);
        foreach (var line in lines.Skip(1))
            Assert.StartsWith("•", line.Trim());
    }

    [Fact]
    public void Build_HasNoFields()
    {
        var members = CreateMembers(3);
        var embed = MorkBorgPartyEmbedRenderer.BuildEmbed("Party", members);

        Assert.Empty(embed.Fields);
    }

    private static List<ICharacter> CreateMembers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => (ICharacter)new FakeCharacter { Name = $"Char{i}" })
            .ToList();
    }
}
