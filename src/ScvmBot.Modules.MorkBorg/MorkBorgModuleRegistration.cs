using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScvmBot.Games.MorkBorg.Generation;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Bootstraps the MÖRK BORG game module: loads reference data (fail-fast),
/// then registers all module services with the DI container.
/// Discovered automatically by the bot via assembly scanning for <see cref="IModuleRegistration"/>.
/// </summary>
public sealed class MorkBorgModuleRegistration : IModuleRegistration
{
    private const string ModuleKey = "MorkBorg";

    private string? _dataPath;
    private MorkBorgReferenceDataService? _refData;
    private string? _pdfTemplatePath;

    /// <summary>Parameterless constructor used by automatic discovery.</summary>
    public MorkBorgModuleRegistration() : this(null) { }

    /// <summary>Constructor for tests that need a custom data directory.</summary>
    public MorkBorgModuleRegistration(string? dataPath) => _dataPath = dataPath;

    /// <inheritdoc />
    public void Configure(IConfiguration configuration)
    {
        // Module-specific section takes precedence over the global fallback.
        var dataPath = configuration[$"Modules:{ModuleKey}:DataPath"]
                    ?? configuration["Modules:DataPath"];
        if (dataPath is { Length: > 0 })
            _dataPath = dataPath;
    }

    public async Task InitializeAsync()
    {
        _refData = _dataPath is not null
            ? await MorkBorgReferenceDataService.CreateAsync(_dataPath)
            : await MorkBorgReferenceDataService.CreateAsync();

        // Resolve PDF template path: when a custom DataPath is configured, look
        // there first so a self-contained data directory works end-to-end.
        // Fall back to the default build-output location.
        _pdfTemplatePath = ResolvePdfTemplatePath();

        if (!File.Exists(_pdfTemplatePath))
        {
            Console.Error.WriteLine(
                $"[MorkBorg] WARNING: PDF template not found at '{_pdfTemplatePath}'. " +
                $"PDF character sheet generation will be disabled. " +
                $"If this is unexpected, check your build output or Data/ directory.");
        }
    }

    private string ResolvePdfTemplatePath()
    {
        if (_dataPath is not null)
        {
            var customPath = Path.Combine(_dataPath, "character_sheet.pdf");
            if (File.Exists(customPath))
                return customPath;
        }

        return MorkBorgPdfRenderer.DefaultTemplatePath;
    }

    public void Register(IServiceCollection services)
    {
        if (_refData is null)
            throw new InvalidOperationException("InitializeAsync() must be called before Register().");

        services.AddSingleton(_refData);
        services.AddSingleton<CharacterGenerator>();
        services.AddSingleton(new MorkBorgPdfRenderer(_pdfTemplatePath!));
        services.AddSingleton<IGameModule, MorkBorgModule>();

        // Renderers
        services.AddSingleton<IResultRenderer, MorkBorgCharacterEmbedRenderer>();
        services.AddSingleton<IResultRenderer, MorkBorgPartyEmbedRenderer>();
        services.AddSingleton<IResultRenderer, MorkBorgCharacterPdfRenderer>();
        services.AddSingleton<IResultRenderer, MorkBorgPartyPdfRenderer>();
    }

    /// <summary>
    /// Convenience factory that initializes and returns a registration delegate.
    /// Used by tests that need a custom data directory.
    /// </summary>
    public static async Task<Action<IServiceCollection>> CreateAsync(string? dataPath = null)
    {
        var reg = new MorkBorgModuleRegistration(dataPath);
        await reg.InitializeAsync();
        return reg.Register;
    }
}
