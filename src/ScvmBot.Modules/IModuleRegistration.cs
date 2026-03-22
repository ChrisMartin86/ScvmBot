using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ScvmBot.Modules;

/// <summary>
/// Implemented by each game module assembly to support automatic discovery.
/// <para>
/// <strong>Naming requirement:</strong> the containing assembly must be named
/// <c>ScvmBot.Modules.{SystemName}</c> (e.g. <c>ScvmBot.Modules.MorkBorg</c>).
/// Assemblies that do not match this prefix are invisible to module discovery
/// and will be silently ignored at startup.
/// </para>
/// The host scans the dependency graph for matching assemblies, locates concrete
/// implementations, calls <see cref="InitializeAsync"/> to perform any async
/// validation (e.g. loading data files), then calls <see cref="Register"/> to
/// wire services into the DI container.
/// Implementations must have a public parameterless constructor.
/// </summary>
public interface IModuleRegistration
{
    /// <summary>
    /// Applies host-supplied configuration before initialization.
    /// The full <see cref="IConfiguration"/> tree is passed so each module can
    /// navigate to its own section (e.g. <c>Modules:MorkBorg</c>) and read
    /// typed, hierarchical settings without key collisions.
    /// The default implementation is a no-op.
    /// Called after construction, before <see cref="InitializeAsync"/>.
    /// </summary>
    void Configure(IConfiguration configuration) { }

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
