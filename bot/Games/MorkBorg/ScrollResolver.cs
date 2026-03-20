namespace ScvmBot.Bot.Games.MorkBorg;

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
        var sacred = _refData.Scrolls.Where(s => s.ScrollType.Equals("Sacred", StringComparison.OrdinalIgnoreCase)).ToList();
        var unclean = _refData.Scrolls.Where(s => s.ScrollType.Equals("Unclean", StringComparison.OrdinalIgnoreCase)).ToList();

        var all = sacred.Concat(unclean).ToList();

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
            if (scrollKey == "random_unclean")
            {
                var scroll = _refData.GetRandomScroll("Unclean", _rng);
                if (scroll != null) scrollsList.Add(scroll.ToFormattedString());
            }
            else if (scrollKey == "random_sacred")
            {
                var scroll = _refData.GetRandomScroll("Sacred", _rng);
                if (scroll != null) scrollsList.Add(scroll.ToFormattedString());
            }
            else if (scrollKey == "random_any_scroll")
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
            case "random_sacred_scroll":
                var sacredScroll = _refData.GetRandomScroll("Sacred", _rng);
                if (sacredScroll != null && scrollsList != null)
                    scrollsList.Add(sacredScroll.ToFormattedString());
                return true;

            case "random_unclean_scroll":
                var uncleanScroll = _refData.GetRandomScroll("Unclean", _rng);
                if (uncleanScroll != null && scrollsList != null)
                    scrollsList.Add(uncleanScroll.ToFormattedString());
                return true;

            case "random_any_scroll":
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
