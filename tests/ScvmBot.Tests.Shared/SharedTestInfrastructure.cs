namespace ScvmBot.Tests.Shared;

public static class SharedTestInfrastructure
{
    public static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var solutionPath = Path.Combine(current.FullName, "ScvmBot.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    public static string GetBotProjectPath() =>
        Path.Combine(GetRepositoryRoot(), "src", "ScvmBot.Bot");

    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ScvmBotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

public sealed class DeterministicRandom : Random
{
    private readonly Queue<int> _values;

    public DeterministicRandom(IEnumerable<int> values)
    {
        _values = new Queue<int>(values);
    }

    public override int Next(int minValue, int maxValue)
    {
        if (maxValue <= minValue)
        {
            return minValue;
        }

        if (_values.Count == 0)
        {
            throw new InvalidOperationException(
                $"DeterministicRandom queue is exhausted. Roll requested: Next({minValue}, {maxValue})");
        }

        var candidate = _values.Dequeue();
        if (candidate < minValue)
        {
            return minValue;
        }

        if (candidate >= maxValue)
        {
            return maxValue - 1;
        }

        return candidate;
    }
}

public static class TestDataBuilder
{
    private static readonly string[] MinimalDataFiles =
        ["classes.json", "spells.json", "names.json", "weapons.json", "armor.json", "items.json"];

    public static async Task<string> CreateMinimalDataDirectoryAsync()
    {
        var dir = SharedTestInfrastructure.CreateTempDirectory();
        foreach (var file in MinimalDataFiles)
            await File.WriteAllTextAsync(Path.Combine(dir, file), "[]");
        return dir;
    }

    public static string GetRealDataDirectoryPath() =>
        Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg", "Data");

    public static string GetRealPdfDataDirectoryPath() =>
        Path.Combine(SharedTestInfrastructure.GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg.Pdf", "Data");
}
