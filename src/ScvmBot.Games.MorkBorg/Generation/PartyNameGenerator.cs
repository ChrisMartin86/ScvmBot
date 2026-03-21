using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Generation;

/// <summary>Generates random party names for groups of adventurers.</summary>
public static class PartyNameGenerator
{
    // Party name template patterns that accept a name or adjective
    private static readonly string[] NamePatterns = new[]
    {
        "{0}'s Crew",
        "{0}'s Company",
        "The {0} Band",
        "Fellowship of {0}",
        "The {0} Mercenaries",
        "{0} and Companions",
        "The {0} Alliance",
        "Brotherhood of {0}",
        "The {0} Order",
        "{0}'s Circle",
    };

    /// <summary>Generates a random party name using character data or a supplied name.</summary>
    public static string Generate(IReadOnlyList<Character> characters, string? suppliedName = null, Random? rng = null)
    {
        if (!string.IsNullOrWhiteSpace(suppliedName))
        {
            return suppliedName;
        }

        rng ??= Random.Shared;

        // Use the first character's name or a pattern from multiple characters
        if (characters.Count == 0)
        {
            return GenerateDefaultName(rng);
        }

        var selectedName = characters[rng.Next(characters.Count)].Name;
        var pattern = NamePatterns[rng.Next(NamePatterns.Length)];
        return string.Format(pattern, selectedName);
    }

    private static string GenerateDefaultName(Random rng)
    {
        var adjectives = new[] { "Brave", "Bold", "Fearless", "Stalwart", "Grim", "Dark", "Lost", "Fallen" };
        var nouns = new[] { "Wanderers", "Seekers", "Survivors", "Outcasts", "Abjured", "Cursed" };

        var adj = adjectives[rng.Next(adjectives.Length)];
        var noun = nouns[rng.Next(nouns.Length)];

        return $"The {adj} {noun}";
    }
}
