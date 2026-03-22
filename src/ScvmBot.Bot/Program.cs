using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Services;
using ScvmBot.Bot.Services.Commands;
using ScvmBot.Modules;
using System.Diagnostics.CodeAnalysis;

namespace ScvmBot.Bot;

[ExcludeFromCodeCoverage(Justification = "Application entry point; bootstrapping code is not unit-testable.")]
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Use the application builder so that the full config pipeline
        // (appsettings, env vars, CLI args, secrets) is available before
        // modules initialize. No separate ConfigurationBuilder needed.
        var builder = Host.CreateApplicationBuilder(args);

        // Discover and initialize game modules via the shared bootstrapper.
        // Each module navigates to its own config section (e.g. Modules:MorkBorg).
        List<IModuleRegistration> modules;
        try
        {
            modules = await ModuleBootstrapper.DiscoverAndInitializeAsync(builder.Configuration);
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

        foreach (var module in modules)
            module.Register(builder.Services);

        // Rendering infrastructure
        builder.Services.AddSingleton<RendererRegistry>();

        // Slash commands registered via ISlashCommand; BotService discovers them automatically.
        builder.Services.AddSingleton<ISlashCommand, HelloCommand>();
        builder.Services.AddSingleton<GenerationDeliveryService>();
        builder.Services.AddSingleton<GenerateCommandHandler>();
        builder.Services.AddSingleton<ISlashCommand>(sp => sp.GetRequiredService<GenerateCommandHandler>());

        builder.Services.AddSingleton<DiscordSocketClient>(_ => new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.DirectMessages
        }));
        builder.Services.AddHostedService<BotService>();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        var host = builder.Build();
        await host.RunAsync();

        return 0;
    }
}
