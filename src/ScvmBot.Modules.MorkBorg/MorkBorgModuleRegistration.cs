using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Bootstraps the MÖRK BORG game module: loads reference data (fail-fast),
/// then returns a registration action that wires all module services into
/// the DI container. No mutable state is retained after initialization.
/// Discovered automatically by the bot via assembly scanning for <see cref="IModuleRegistration"/>.
/// </summary>
public sealed class MorkBorgModuleRegistration : IModuleRegistration
{
    private const string ModuleKey = "MorkBorg";

    public async Task<Action<IServiceCollection>> InitializeAsync(IConfiguration configuration, ILogger? logger = null)
    {
        var dataPath = configuration[$"Modules:{ModuleKey}:DataPath"]
                    ?? configuration["Modules:DataPath"];

        var refData = dataPath is { Length: > 0 }
            ? await MorkBorgReferenceDataService.CreateAsync(dataPath)
            : await MorkBorgReferenceDataService.CreateAsync();

        var pdfTemplatePath = ResolvePdfTemplatePath(dataPath);

        if (!File.Exists(pdfTemplatePath))
        {
            logger?.LogWarning(
                "PDF template not found at '{PdfTemplatePath}'. " +
                "PDF character sheet generation will be disabled. " +
                "If this is unexpected, check your build output or Data/ directory.",
                pdfTemplatePath);
        }

        return services =>
        {
            services.AddSingleton(Random.Shared);
            services.AddSingleton(refData);
            services.AddSingleton<MorkBorgRandomPicker>();
            services.AddSingleton<DiceRoller>();
            services.AddSingleton<AbilityRoller>();
            services.AddSingleton<WeaponResolver>();
            services.AddSingleton<ArmorResolver>();
            services.AddSingleton<ScrollResolver>();
            services.AddSingleton<StartingGearTable>();
            services.AddSingleton(sp => new VignetteGenerator(sp.GetRequiredService<MorkBorgReferenceDataService>().Vignettes));
            services.AddSingleton<CharacterGenerator>();
            services.AddSingleton(new MorkBorgPdfRenderer(pdfTemplatePath));
            services.AddSingleton<IGameModule, MorkBorgModule>();

            // Renderers
            services.AddSingleton<IResultRenderer, MorkBorgCharacterEmbedRenderer>();
            services.AddSingleton<IResultRenderer, MorkBorgCharacterPdfRenderer>();
        };
    }

    private static string ResolvePdfTemplatePath(string? dataPath)
    {
        if (dataPath is not null)
        {
            var customPath = Path.Combine(dataPath, "character_sheet.pdf");
            if (File.Exists(customPath))
                return customPath;
        }

        return MorkBorgPdfRenderer.DefaultTemplatePath;
    }
}
