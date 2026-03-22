using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using System.Reflection;

namespace ScvmBot.Modules;

/// <summary>
/// Shared module discovery and initialization logic used by both the bot and CLI hosts.
/// Reads the build manifest (<c>.deps.json</c>) to discover module assemblies
/// matching <c>ScvmBot.Modules.*</c>, then locates <see cref="IModuleRegistration"/>
/// implementations, applies configuration, and initialises each module.
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
    /// section (e.g. <c>Modules:MorkBorg</c>) inside its <c>Configure</c> method.
    /// </param>
    public static async Task<List<IModuleRegistration>> DiscoverAndInitializeAsync(
        IConfiguration configuration)
    {
        var registrationTypes = GetModuleAssemblies()
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => typeof(IModuleRegistration).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && !t.IsInterface);

        var modules = new List<IModuleRegistration>();
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
            module.Configure(configuration);
            await module.InitializeAsync();
            modules.Add(module);
        }

        return modules;
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
