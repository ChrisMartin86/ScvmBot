using System.Reflection;

namespace ScvmBot.Games.MorkBorg.Tests;

internal static class TestUtilities
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

    public static string GetBotProjectPath()
    {
        return Path.Combine(GetRepositoryRoot(), "bot");
    }

    public static string GetMorkBorgDataPath()
    {
        return Path.Combine(GetRepositoryRoot(), "games", "ScvmBot.Games.MorkBorg", "Data");
    }

    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ScvmBotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? throw new MissingMethodException(instance.GetType().Name, methodName);

        var result = method.Invoke(instance, args);
        return (T)result!;
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
            return minValue;
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
