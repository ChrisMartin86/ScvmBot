using ScvmBot.Games.CyBorg.Models;

namespace ScvmBot.Games.CyBorg.Generation;

/// <summary>Generates random group names for groups of cy_borg crew members.</summary>
public static class CyBorgGroupNameGenerator
{
    private static readonly string[] NamePatterns =
    [
        "{0}'s Crew",
        "{0}'s Squad",
        "The {0} Gang",
        "{0} and Associates",
        "The {0} Syndicate",
        "{0}'s Network",
        "Team {0}",
        "{0}'s Operatives",
        "The {0} Collective",
        "{0}'s Run",
    ];

    /// <summary>Generates a random group name using character data or a supplied name.</summary>
    public static string Generate(IReadOnlyList<CyBorgCharacter> characters, string? suppliedName = null, Random? rng = null)
    {
        if (!string.IsNullOrWhiteSpace(suppliedName))
        {
            return suppliedName;
        }

        rng ??= Random.Shared;

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
        var adjectives = new[] { "Chrome", "Ghost", "Static", "Neon", "Broken", "Wired", "Rogue", "Burned" };
        var nouns = new[] { "Runners", "Hackers", "Punks", "Outcasts", "Ghosts", "Operators" };

        var adj = adjectives[rng.Next(adjectives.Length)];
        var noun = nouns[rng.Next(nouns.Length)];

        return $"The {adj} {noun}";
    }
}
