using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Tests;

public class MorkBorgGameRulesFixture
{
    protected static async Task<MorkBorgReferenceDataService> LoadGameReferenceDataAsync()
    {
        var dataRoot = GetDataRootPath();
        return await MorkBorgReferenceDataService.CreateAsync(dataRoot);
    }

    protected static string GetDataRootPath()
    {
        var repoRoot = SharedTestInfrastructure.GetRepositoryRoot();
        return Path.Combine(repoRoot, "games", "ScvmBot.Games.MorkBorg", "Data");
    }
}
