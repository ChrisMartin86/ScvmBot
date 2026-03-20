using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Games.MorkBorg;
using ScvmBot.Bot.Rendering;
using ScvmBot.Bot.Services;
using ScvmBot.Bot.Services.Commands;
using ScvmBot.Games.MorkBorg.Reference;
using System.Diagnostics.CodeAnalysis;

namespace ScvmBot.Bot;

[ExcludeFromCodeCoverage(Justification = "Application entry point; bootstrapping code is not unit-testable.")]
class Program
{
    static async Task Main(string[] args)
    {
        // Load reference data before building the host so missing or invalid data fails fast.
        // Wrap in explicit error handling so operators see a clear message rather than a raw exception.
        MorkBorgReferenceDataService referenceData;
        try
        {
            referenceData = await MorkBorgReferenceDataService.CreateAsync();
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine("[ScvmBot] Startup failed: a required data file is missing.");
            Console.Error.WriteLine($"  {ex.Message}");
            Console.Error.WriteLine("  Ensure the Data/ directory is present and contains all required JSON files.");
            return;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine("[ScvmBot] Startup failed: reference data could not be loaded.");
            Console.Error.WriteLine($"  {ex.Message}");
            return;
        }

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddMorkBorgServices(referenceData);

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
    }
}
