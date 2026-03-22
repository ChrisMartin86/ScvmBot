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
        // Support array-style config (Discord:GuildIds:0, Discord:GuildIds:1, ...)
        var section = configuration.GetSection("Discord:GuildIds");
        if (!section.Exists())
            return new RegistrationStrategy(RegistrationMode.Global, []);

        var children = section.GetChildren().ToList();

        // A scalar value like "id1,id2" means someone used the wrong format.
        if (children.Count == 0 && section.Value is not null)
        {
            throw new InvalidOperationException(
                $"Discord:GuildIds must be configured as an array " +
                $"(Discord:GuildIds:0, Discord:GuildIds:1, ...), not a scalar value. " +
                $"Found: \"{section.Value}\"");
        }

        // Empty section with no children and no value — treat as unconfigured.
        if (children.Count == 0)
            return new RegistrationStrategy(RegistrationMode.Global, []);

        // Every entry in the array must be a valid, non-zero guild ID.
        // A typo here could silently downgrade guild-only to global registration.
        var guildIds = new List<ulong>();
        foreach (var child in children)
        {
            if (!ulong.TryParse(child.Value, out var id) || id == 0)
            {
                throw new InvalidOperationException(
                    $"Discord:GuildIds contains an invalid entry at key '{child.Key}': " +
                    $"\"{child.Value}\". Each entry must be a valid non-zero guild ID.");
            }

            guildIds.Add(id);
        }

        return new RegistrationStrategy(RegistrationMode.Guild, guildIds);
    }
}
