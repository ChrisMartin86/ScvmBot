using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Bot.Games.MorkBorg;

public sealed class VignetteGenerator
{
    private readonly VignetteData _data;
    private readonly Random _rng;

    public VignetteGenerator(VignetteData data, Random? rng = null)
    {
        _data = data;
        _rng = rng ?? Random.Shared;
    }

    public string Generate(Character character)
    {
        if (_data.Templates.Count == 0)
            return string.Empty;

        var template = PickRandom(_data.Templates);

        var result = template
            .Replace("{name}", character.Name)
            .Replace("{classIntro}", PickClassIntro(character.ClassName))
            .Replace("{body}", PickFromKeyed(_data.Bodies, ExtractDescription(character, "Body")))
            .Replace("{habit}", PickFromKeyed(_data.Habits, ExtractDescription(character, "Habit")))
            .Replace("{item}", PickFromKeyed(_data.Items, ExtractWeaponName(character)))
            .Replace("{trait}", PickFromKeyed(_data.Traits, ExtractDescription(character, "Trait")))
            .Replace("{closer}", PickRandom(_data.Closers));

        return CapitalizeSentenceStarts(result);
    }

    private static string? ExtractDescription(Character character, string prefix)
    {
        foreach (var desc in character.Descriptions)
        {
            if (desc.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase))
                return desc[(prefix.Length + 1)..].Trim();
        }
        return null;
    }

    private static string? ExtractWeaponName(Character character)
    {
        if (string.IsNullOrWhiteSpace(character.EquippedWeapon))
            return null;
        var paren = character.EquippedWeapon.IndexOf('(');
        return paren > 0
            ? character.EquippedWeapon[..paren].Trim()
            : character.EquippedWeapon.Trim();
    }

    private string PickFromKeyed(Dictionary<string, List<string>> dict, string? key)
    {
        if (key != null && dict.TryGetValue(key, out var pool) && pool.Count > 0)
            return pool[_rng.Next(pool.Count)];

        // Fallback: pick from any random entry
        if (dict.Count > 0)
        {
            var values = dict.Values.ToList();
            var bucket = values[_rng.Next(values.Count)];
            if (bucket.Count > 0)
                return bucket[_rng.Next(bucket.Count)];
        }

        return "";
    }

    private string PickClassIntro(string? className)
    {
        var key = string.IsNullOrWhiteSpace(className) ? "Classless" : className;

        if (_data.ClassIntros.TryGetValue(key, out var pool) && pool.Count > 0)
            return pool[_rng.Next(pool.Count)];

        if (_data.ClassIntros.TryGetValue("Default", out var fallback) && fallback.Count > 0)
            return fallback[_rng.Next(fallback.Count)];

        return "a scvm";
    }

    private string PickRandom(List<string> pool)
    {
        return pool.Count > 0 ? pool[_rng.Next(pool.Count)] : "";
    }

    private static string CapitalizeSentenceStarts(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new System.Text.StringBuilder(text);

        for (int i = 0; i < sb.Length - 2; i++)
        {
            if (sb[i] == '.' && sb[i + 1] == ' ' && char.IsLower(sb[i + 2]))
                sb[i + 2] = char.ToUpperInvariant(sb[i + 2]);
        }

        return sb.ToString();
    }
}
