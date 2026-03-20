using Discord;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Renders a MÖRK BORG <see cref="PartyGenerationResult"/> as a Discord embed
/// showing the party name, size, and roster.
/// </summary>
public sealed class MorkBorgPartyEmbedRenderer : IResultRenderer
{
    private static readonly Color PartyColor = new(150, 20, 20);

    public OutputFormat Format => OutputFormat.DiscordEmbed;

    public bool CanRender(GenerateResult result) =>
        result is PartyGenerationResult;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not PartyGenerationResult partyResult)
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a MÖRK BORG party embed.");

        return new EmbedOutput(BuildEmbed(partyResult.PartyName, partyResult.Characters));
    }

    internal static Embed BuildEmbed(string partyName, IReadOnlyList<ICharacter> members)
    {
        var memberList = string.Join("\n", members.Select(m => $"• {m.Name}"));
        var description = $"Party of {members.Count}\n\n{memberList}";

        var embed = new EmbedBuilder()
            .WithTitle(partyName)
            .WithDescription(description)
            .WithColor(PartyColor)
            .WithFooter("MÖRK BORG is © Ockult Örtmästare Games & Stockholm Kartell. Used under the MÖRK BORG Third Party License.");

        return embed.Build();
    }
}
