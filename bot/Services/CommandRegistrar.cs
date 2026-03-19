using Microsoft.Extensions.Configuration;

namespace ScvmBot.Bot.Services;

public enum RegistrationMode
{
    Global,
    Guild
}

public record RegistrationStrategy(RegistrationMode Mode, ulong? GuildId);

public static class CommandRegistrar
{
    public static RegistrationStrategy ResolveStrategy(IConfiguration configuration)
    {
        var guildId = configuration.GetValue<ulong?>("Discord:GuildId");
        if (guildId is > 0)
            return new RegistrationStrategy(RegistrationMode.Guild, guildId.Value);

        return new RegistrationStrategy(RegistrationMode.Global, null);
    }
}
