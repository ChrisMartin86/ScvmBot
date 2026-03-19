using ScvmBot.Bot.Games.MorkBorg;

namespace ScvmBot.Games.MorkBorg.Tests;

public class MorkBorgGameRulesFixture
{
    protected static async Task<MorkBorgReferenceDataService> LoadGameReferenceDataAsync()
    {
        var dataRoot = GetDataRootPath();
        var service = new MorkBorgReferenceDataService(dataRoot);
        await service.LoadDataAsync();
        return service;
    }

    protected static string GetDataRootPath()
    {
        var repoRoot = FindRepositoryRoot();
        return Path.Combine(repoRoot, "bot", "Data", "MorkBorg");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ScvmBot.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (ScvmBot.sln).");
    }
}
