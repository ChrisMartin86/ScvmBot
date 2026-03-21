using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Services;
using ScvmBot.Bot.Services.Commands;
using ScvmBot.Modules;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ScvmBot.Bot;

[ExcludeFromCodeCoverage(Justification = "Application entry point; bootstrapping code is not unit-testable.")]
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Discover and initialize game modules via assembly scanning.
        // Each module performs its own startup validation; failures are fatal.
        List<IModuleRegistration> modules;
        try
        {
            modules = await DiscoverAndInitializeModulesAsync();
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine("[ScvmBot] Startup failed: a required data file is missing.");
            Console.Error.WriteLine($"  {ex.Message}");
            Console.Error.WriteLine("  Ensure the Data/ directory is present and contains all required JSON files.");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine("[ScvmBot] Startup failed: module could not be initialized.");
            Console.Error.WriteLine($"  {ex.Message}");
            return 1;
        }

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                foreach (var module in modules)
                    module.Register(services);

                // Rendering infrastructure
                services.AddSingleton<RendererRegistry>();

                // Slash commands registered via ISlashCommand; BotService discovers them automatically.
                services.AddSingleton<ISlashCommand, HelloCommand>();
                services.AddSingleton<GenerationDeliveryService>();
                services.AddSingleton<GenerateCommandHandler>();
                services.AddSingleton<ISlashCommand>(sp => sp.GetRequiredService<GenerateCommandHandler>());

                services.AddSingleton<DiscordSocketClient>(_ => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.DirectMessages
                }));
                services.AddHostedService<BotService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();

        await host.RunAsync();

        return 0;
    }

    /// <summary>
    /// Scans the application base directory for module assemblies (ScvmBot.Modules.*.dll),
    /// discovers <see cref="IModuleRegistration"/> implementations, instantiates each via
    /// its parameterless constructor, and calls <see cref="IModuleRegistration.InitializeAsync"/>.
    /// </summary>
    private static async Task<List<IModuleRegistration>> DiscoverAndInitializeModulesAsync()
    {
        var settings = new Dictionary<string, string>();

        var baseDir = AppContext.BaseDirectory;
        var registrationTypes = Directory.GetFiles(baseDir, "ScvmBot.Modules.*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => { try { return Assembly.Load(name!); } catch { return null; } })
            .Where(a => a is not null)
            .SelectMany(a => a!.GetExportedTypes())
            .Where(t => typeof(IModuleRegistration).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && !t.IsInterface);

        var modules = new List<IModuleRegistration>();
        foreach (var type in registrationTypes)
        {
            var module = (IModuleRegistration)Activator.CreateInstance(type)!;
            if (settings.Count > 0)
                module.Configure(settings);
            await module.InitializeAsync();
            modules.Add(module);
        }

        return modules;
    }
}
