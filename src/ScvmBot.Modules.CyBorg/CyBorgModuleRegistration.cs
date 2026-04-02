using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScvmBot.Games.CyBorg.Generation;
using ScvmBot.Games.CyBorg.Reference;

namespace ScvmBot.Modules.CyBorg;

/// <summary>
/// Bootstraps the Cy_Borg game module: loads reference data (fail-fast),
/// then returns a registration action that wires all module services into
/// the DI container. No mutable state is retained after initialization.
/// Discovered automatically by the bot via assembly scanning for <see cref="IModuleRegistration"/>.
/// </summary>
public sealed class CyBorgModuleRegistration : IModuleRegistration
{
    private const string ModuleKey = "CyBorg";

    public async Task<Action<IServiceCollection>> InitializeAsync(IConfiguration configuration, ILogger? logger = null)
    {
        var dataPath = configuration[$"Modules:{ModuleKey}:DataPath"]
                    ?? configuration["Modules:DataPath"];

        var refData = dataPath is { Length: > 0 }
            ? await CyBorgReferenceDataService.CreateAsync(dataPath)
            : await CyBorgReferenceDataService.CreateAsync();

        return services =>
        {
            services.AddSingleton(Random.Shared);
            services.AddSingleton(refData);
            services.AddSingleton<CyBorgRandomPicker>();
            services.AddSingleton<CyBorgDiceRoller>();
            services.AddSingleton<CyBorgAbilityRoller>();
            services.AddSingleton<CyBorgCharacterGenerator>();
            services.AddSingleton<IGameModule, CyBorgModule>();

            // Renderers
            services.AddSingleton<IResultRenderer, CyBorgCharacterEmbedRenderer>();
        };
    }
}
