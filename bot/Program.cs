using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Games;
using ScvmBot.Bot.Games.MorkBorg;
using ScvmBot.Bot.Models;
using ScvmBot.Bot.Services;
using ScvmBot.Bot.Services.Commands;
using System.Diagnostics.CodeAnalysis;

namespace ScvmBot.Bot;

[ExcludeFromCodeCoverage(Justification = "Application entry point; bootstrapping code is not unit-testable.")]
class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                config.AddEnvironmentVariables();

                config.AddInMemoryCollection(MapEnvironmentVariables());
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MorkBorgReferenceDataService>();
                services.AddSingleton<CharacterGenerator>();
                services.AddSingleton<IGameSystem, MorkBorgGameSystem>();

                // Slash commands registered via ISlashCommand; BotService discovers them automatically.
                services.AddSingleton<ISlashCommand, HelloCommand>();
                services.AddSingleton<GenerateCommandHandler>();
                services.AddSingleton<ISlashCommand>(sp => sp.GetRequiredService<GenerateCommandHandler>());

                services.AddSingleton<DiscordSocketClient>(_ => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.DirectMessages
                }));
                services.Configure<GuildSettings>(context.Configuration.GetSection("Guild"));
                services.AddHostedService<BotService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

        var refDataService = host.Services.GetRequiredService<MorkBorgReferenceDataService>();
        await refDataService.LoadDataAsync();
        logger.LogInformation("Reference data loaded successfully.");

        await host.RunAsync();
    }

    private static Dictionary<string, string?> MapEnvironmentVariables()
    {
        var map = new Dictionary<string, string?>();

        void Map(string envVar, string configKey)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(value))
                map[configKey] = value;
        }

        Map("DISCORD_TOKEN", "Discord:Token");
        Map("BOT_SYNC_COMMANDS", "Bot:SyncCommands");
        Map("DISCORD_GUILD_ID", "Discord:GuildId");

        return map;
    }
}
