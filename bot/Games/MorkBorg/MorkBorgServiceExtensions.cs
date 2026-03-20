using Microsoft.Extensions.DependencyInjection;
using ScvmBot.Bot.Games;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Bot.Games.MorkBorg;

/// <summary>
/// Registers all MÖRK BORG game services with the DI container.
/// Reference data is loaded before the host is built so missing or invalid data files
/// cause an immediate startup failure rather than a deferred runtime error.
/// </summary>
internal static class MorkBorgServiceExtensions
{
    internal static IServiceCollection AddMorkBorgServices(
        this IServiceCollection services,
        MorkBorgReferenceDataService referenceData)
    {
        services.AddSingleton(referenceData);
        services.AddSingleton<CharacterGenerator>();
        services.AddSingleton<MorkBorgPdfRenderer>();
        services.AddSingleton<IGameSystem, MorkBorgGameSystem>();
        return services;
    }
}
