using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Generation;

public sealed class ScrollResolver
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly Random _rng;

    public ScrollResolver(MorkBorgReferenceDataService refData, Random rng)
    {
        _refData = refData;
        _rng = rng;
    }

    public string GetRandomAnyScroll()
    {
        var all = _refData.Scrolls
            .Where(s => s.Kind == ScrollKind.Sacred || s.Kind == ScrollKind.Unclean)
            .ToList();

        if (all.Count == 0)
        {
            throw new InvalidOperationException("No sacred or unclean scrolls are available.");
        }

        return all[_rng.Next(all.Count)].ToFormattedString();
    }

    public void ResolveStartingScrolls(ClassData classData, List<string> scrollsList)
    {
        if (classData.StartingScrolls == null)
            return;

        foreach (var scrollKey in classData.StartingScrolls)
        {
            if (scrollKey == MorkBorgConstants.ScrollToken.RandomUnclean)
            {
                var scroll = _refData.GetRandomScroll(ScrollKind.Unclean, _rng);
                if (scroll is null)
                    throw new InvalidOperationException(
                        $"Class '{classData.Name}' requires an Unclean scroll but no Unclean scrolls exist in the data.");
                scrollsList.Add(scroll.ToFormattedString());
            }
            else if (scrollKey == MorkBorgConstants.ScrollToken.RandomSacred)
            {
                var scroll = _refData.GetRandomScroll(ScrollKind.Sacred, _rng);
                if (scroll is null)
                    throw new InvalidOperationException(
                        $"Class '{classData.Name}' requires a Sacred scroll but no Sacred scrolls exist in the data.");
                scrollsList.Add(scroll.ToFormattedString());
            }
            else if (scrollKey == MorkBorgConstants.ScrollToken.RandomAnyScroll)
            {
                var scrollName = GetRandomAnyScroll();
                if (!string.IsNullOrEmpty(scrollName)) scrollsList.Add(scrollName);
            }
            else
            {
                scrollsList.Add(scrollKey);
            }
        }
    }

    public bool TryProcessStartingItemToken(string token, List<string>? scrollsList)
    {
        switch (token.ToLowerInvariant())
        {
            case MorkBorgConstants.ScrollToken.RandomSacredScroll:
                var sacredScroll = _refData.GetRandomScroll(ScrollKind.Sacred, _rng);
                if (sacredScroll is null)
                    throw new InvalidOperationException(
                        "A Sacred scroll is required by starting item data but no Sacred scrolls exist in the data.");
                scrollsList?.Add(sacredScroll.ToFormattedString());
                return true;

            case MorkBorgConstants.ScrollToken.RandomUncleanScroll:
                var uncleanScroll = _refData.GetRandomScroll(ScrollKind.Unclean, _rng);
                if (uncleanScroll is null)
                    throw new InvalidOperationException(
                        "An Unclean scroll is required by starting item data but no Unclean scrolls exist in the data.");
                scrollsList?.Add(uncleanScroll.ToFormattedString());
                return true;

            case MorkBorgConstants.ScrollToken.RandomAnyScroll:
                var anyScroll = GetRandomAnyScroll();
                if (!string.IsNullOrEmpty(anyScroll) && scrollsList != null)
                    scrollsList.Add(anyScroll);
                return true;

            default:
                if (token.StartsWith("random_", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Unsupported generation token in startingItems: '{token}'");
                return false;
        }
    }
}
