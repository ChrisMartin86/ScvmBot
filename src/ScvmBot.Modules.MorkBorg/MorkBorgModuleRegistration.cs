using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    public async Task<Action<IServiceCollection>> InitializeAsync(IConfiguration configuration)
    {
        var dataPath = configuration[$"Modules:{ModuleKey}:DataPath"]
                    ?? configuration["Modules:DataPath"];

        var refData = dataPath is { Length: > 0 }
            ? await MorkBorgReferenceDataService.CreateAsync(dataPath)
            : await MorkBorgReferenceDataService.CreateAsync();

        var pdfTemplatePath = ResolvePdfTemplatePath(dataPath);

        if (!File.Exists(pdfTemplatePath))
        {
            Console.Error.WriteLine(
                $"[MorkBorg] WARNING: PDF template not found at '{pdfTemplatePath}'. " +
                $"PDF character sheet generation will be disabled. " +
                $"If this is unexpected, check your build output or Data/ directory.");
        }

        return services =>
        {
            services.AddSingleton(refData);
            services.AddSingleton<CharacterGenerator>();
            services.AddSingleton(new MorkBorgPdfRenderer(pdfTemplatePath));
            services.AddSingleton<IGameModule, MorkBorgModule>();

            // Renderers
            services.AddSingleton<IResultRenderer, MorkBorgCharacterEmbedRenderer>();
            services.AddSingleton<IResultRenderer, MorkBorgPartyEmbedRenderer>();
            services.AddSingleton<IResultRenderer, MorkBorgCharacterPdfRenderer>();
            services.AddSingleton<IResultRenderer, MorkBorgPartyPdfRenderer>();
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
