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
