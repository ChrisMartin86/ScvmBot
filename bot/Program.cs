using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Services;
using ScvmBot.Bot.Services.Commands;
using ScvmBot.Rendering;
using ScvmBot.Rendering.MorkBorg;
using System.Diagnostics.CodeAnalysis;

namespace ScvmBot.Bot;

[ExcludeFromCodeCoverage(Justification = "Application entry point; bootstrapping code is not unit-testable.")]
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Initialize game modules before building the host.
        // Each module performs its own startup validation; failures are fatal.
        Action<IServiceCollection> registerMorkBorg;
        try
        {
            registerMorkBorg = await MorkBorgModuleRegistration.CreateAsync();
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
                // Game modules — add new modules here
                registerMorkBorg(services);

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
}
