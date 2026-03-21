using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Modules.MorkBorg;

/// <summary>
/// Renders a MÖRK BORG <see cref="PartyGenerationResult"/> as a structured card
/// showing the party name, size, and roster.
/// </summary>
public sealed class MorkBorgPartyEmbedRenderer : IResultRenderer
{
    private static readonly CardColor PartyColor = new(150, 20, 20);

    public Type ResultType => typeof(PartyGenerationResult<Character>);

    public OutputFormat Format => OutputFormat.Card;

    public bool CanRender(GenerateResult result) =>
        result is PartyGenerationResult<Character>;

    public RenderOutput Render(GenerateResult result)
    {
        if (result is not PartyGenerationResult<Character> partyResult)
            throw new InvalidOperationException(
                $"Cannot render {result.GetType().Name} as a MÖRK BORG party card.");

        return BuildCard(partyResult.PartyName, partyResult.Characters);
    }

    internal static CardOutput BuildCard(string partyName, IReadOnlyList<Character> members)
    {
        var memberList = string.Join("\n", members.Select(m => $"• {m.Name}"));
        var description = $"Party of {members.Count}\n\n{memberList}";

        return new CardOutput(
            Title: partyName,
            Description: description,
            Footer: "MÖRK BORG is © Ockult Örtmästare Games & Stockholm Kartell. Used under the MÖRK BORG Third Party License.",
            Color: PartyColor);
    }
}
