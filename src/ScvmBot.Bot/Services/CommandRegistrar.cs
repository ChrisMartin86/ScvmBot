using Microsoft.Extensions.Configuration;

namespace ScvmBot.Bot.Services;

public enum RegistrationMode
{
    Global,
    Guild
}

public record RegistrationStrategy(RegistrationMode Mode, IReadOnlyList<ulong> GuildIds);

public static class CommandRegistrar
{
    public static RegistrationStrategy ResolveStrategy(IConfiguration configuration)
    {
        var guildIds = new List<ulong>();

        // Support array-style config (Discord:GuildIds:0, Discord:GuildIds:1, ...)
        var section = configuration.GetSection("Discord:GuildIds");
        if (section.Exists())
        {
            foreach (var child in section.GetChildren())
            {
                if (ulong.TryParse(child.Value, out var id) && id > 0)
                    guildIds.Add(id);
            }
        }

        if (guildIds.Count > 0)
            return new RegistrationStrategy(RegistrationMode.Guild, guildIds);

        return new RegistrationStrategy(RegistrationMode.Global, []);
    }
}
