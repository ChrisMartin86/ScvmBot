using Microsoft.Extensions.DependencyInjection;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Rendering.MorkBorg;

/// <summary>
/// Bootstraps the MÖRK BORG game module: loads reference data (fail-fast),
/// then returns a delegate that registers all module services with the DI container.
/// </summary>
public static class MorkBorgModuleRegistration
{
    /// <summary>
    /// Performs async startup validation (reference data loading) and returns
    /// a synchronous delegate that registers all MÖRK BORG services.
    /// Throws immediately if required data files are missing or invalid.
    /// </summary>
    public static async Task<Action<IServiceCollection>> CreateAsync(string? dataPath = null)
    {
        var refData = dataPath is not null
            ? await MorkBorgReferenceDataService.CreateAsync(dataPath)
            : await MorkBorgReferenceDataService.CreateAsync();

        return services =>
        {
            services.AddSingleton(refData);
            services.AddSingleton<CharacterGenerator>();
            services.AddSingleton<MorkBorgPdfRenderer>();
            services.AddSingleton<IGameModule, MorkBorgModule>();

            // Renderers
            services.AddSingleton<IResultRenderer, MorkBorgCharacterEmbedRenderer>();
            services.AddSingleton<IResultRenderer, MorkBorgPartyEmbedRenderer>();
            services.AddSingleton<IResultRenderer, MorkBorgCharacterPdfRenderer>();
            services.AddSingleton<IResultRenderer, MorkBorgPartyPdfRenderer>();
        };
    }
}
