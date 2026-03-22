using System.Reflection;

namespace ScvmBot.Games.MorkBorg.Tests;

internal static class TestUtilities
{
    public static string GetRepositoryRoot() => SharedTestInfrastructure.GetRepositoryRoot();
    public static string GetBotProjectPath() => SharedTestInfrastructure.GetBotProjectPath();
    public static string CreateTempDirectory() => SharedTestInfrastructure.CreateTempDirectory();

    public static string GetMorkBorgDataPath() =>
        Path.Combine(GetRepositoryRoot(), "src", "ScvmBot.Games.MorkBorg", "Data");

    public static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? throw new MissingMethodException(instance.GetType().Name, methodName);

        var result = method.Invoke(instance, args);
        return (T)result!;
    }
}

