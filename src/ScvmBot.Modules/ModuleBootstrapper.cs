using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using System.Reflection;

namespace ScvmBot.Modules;

/// <summary>
/// Shared module discovery and initialization logic used by both the bot and CLI hosts.
/// Reads the build manifest (<c>.deps.json</c>) via <see cref="DependencyContext"/> to
/// discover module assemblies whose names start with <c>ScvmBot.Modules.</c>, then
/// locates <see cref="IModuleRegistration"/> implementations, applies configuration,
/// and initialises each module.
/// <para>
/// <strong>Hard naming rule:</strong> only assemblies named <c>ScvmBot.Modules.{SystemName}</c>
/// are scanned. An assembly that implements <see cref="IModuleRegistration"/> but uses a
/// different naming pattern will never be discovered. This is by design — the convention
/// keeps the base <c>ScvmBot.Modules</c> abstractions assembly out of the scan and
/// provides a predictable discovery boundary.
/// </para>
/// </summary>
public static class ModuleBootstrapper
{
    /// <summary>
    /// Discovers and initialises all <see cref="IModuleRegistration"/> implementations
    /// found in assemblies whose names start with <c>ScvmBot.Modules.</c> in the
    /// application's dependency graph.
    /// </summary>
    /// <param name="configuration">
    /// The full host configuration tree. Each module navigates to its own
    /// section (e.g. <c>Modules:MorkBorg</c>) inside its <c>InitializeAsync</c> method.
    /// </param>
    public static async Task<List<Action<IServiceCollection>>> DiscoverAndInitializeAsync(
        IConfiguration configuration,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        var registrationTypes = GetModuleAssemblies()
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => typeof(IModuleRegistration).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && !t.IsInterface)
            // Sort by assembly-qualified type name to guarantee a stable DI registration order
            // across machines. This is intentionally independent of any user-visible concept.
            .OrderBy(t => t.FullName, StringComparer.OrdinalIgnoreCase);

        return await InitializeFromTypesAsync(registrationTypes, configuration, logger);
    }

    /// <summary>
    /// Instantiates and initialises <see cref="IModuleRegistration"/>
    /// implementations from the provided types. Throws if any type lacks a public
    /// parameterless constructor or if no modules are discovered.
    /// </summary>
    internal static async Task<List<Action<IServiceCollection>>> InitializeFromTypesAsync(
        IEnumerable<Type> registrationTypes,
        IConfiguration configuration,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        var registrations = new List<Action<IServiceCollection>>();
        foreach (var type in registrationTypes)
        {
            if (type.GetConstructor(Type.EmptyTypes) is null)
            {
                throw new InvalidOperationException(
                    $"Module registration type '{type.FullName}' in assembly '{type.Assembly.GetName().Name}' " +
                    $"does not have a public parameterless constructor. " +
                    $"IModuleRegistration implementations must be constructible without arguments.");
            }

            var module = (IModuleRegistration)Activator.CreateInstance(type)!;
            var register = await module.InitializeAsync(configuration, logger);
            registrations.Add(register);
        }

        if (registrations.Count == 0)
        {
            throw new InvalidOperationException(
                "No game modules were discovered. " +
                "Ensure at least one ScvmBot.Modules.* assembly is referenced and contains " +
                "a concrete IModuleRegistration implementation.");
        }

        return registrations;
    }

    private static IEnumerable<Assembly> GetModuleAssemblies()
    {
        var context = DependencyContext.Default;
        if (context is null)
            yield break;

        foreach (var library in context.RuntimeLibraries)
        {
            if (library.Name.StartsWith("ScvmBot.Modules.", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var name in library.GetDefaultAssemblyNames(context))
                    yield return Assembly.Load(name);
            }
        }
    }
}
