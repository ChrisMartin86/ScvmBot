using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
/// implementations, and calls <see cref="InitializeAsync"/> which loads data,
/// validates configuration, and returns a registration action that wires all
/// module services into the DI container.
/// Implementations must have a public parameterless constructor.
/// </summary>
public interface IModuleRegistration
{
    /// <summary>
    /// Performs async startup work (loading data files, validating configuration)
    /// and returns an action that registers all module services with the DI container.
    /// <para>
    /// The full <see cref="IConfiguration"/> tree is passed so each module can
    /// navigate to its own section (e.g. <c>Modules:MorkBorg</c>).
    /// </para>
    /// Throw if required resources are missing or invalid — the host treats any
    /// exception here as a fatal startup failure.
    /// </summary>
    Task<Action<IServiceCollection>> InitializeAsync(IConfiguration configuration, ILogger? logger = null);
}
