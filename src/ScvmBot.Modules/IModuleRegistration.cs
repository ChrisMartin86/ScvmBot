using Microsoft.Extensions.DependencyInjection;

namespace ScvmBot.Modules;

/// <summary>
/// Implemented by each game module assembly to support automatic discovery.
/// The bot scans referenced assemblies for concrete implementations at startup,
/// calls <see cref="InitializeAsync"/> to perform any async validation (e.g. loading
/// data files), then calls <see cref="Register"/> to wire services into the DI container.
/// Implementations must have a public parameterless constructor.
/// </summary>
public interface IModuleRegistration
{
    /// <summary>
    /// Applies host-supplied configuration before initialization.
    /// Modules inspect the dictionary for keys they recognize (e.g. "DataPath")
    /// and ignore everything else. The default implementation is a no-op.
    /// Called after construction, before <see cref="InitializeAsync"/>.
    /// </summary>
    void Configure(IReadOnlyDictionary<string, string> settings) { }

    /// <summary>
    /// Performs async startup validation (e.g. loading reference data).
    /// Throw if required resources are missing or invalid — the bot treats any
    /// exception here as a fatal startup failure.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Registers all module services (<see cref="IGameModule"/>,
    /// <see cref="IResultRenderer"/> implementations, etc.) with the DI container.
    /// Called only after <see cref="InitializeAsync"/> succeeds.
    /// </summary>
    void Register(IServiceCollection services);
}
