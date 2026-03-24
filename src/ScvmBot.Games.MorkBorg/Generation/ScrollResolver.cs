using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Generation;

public sealed class ScrollResolver
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly MorkBorgRandomPicker _picker;

    public ScrollResolver(MorkBorgReferenceDataService refData, MorkBorgRandomPicker picker)
    {
        _refData = refData;
        _picker = picker;
    }

    public string GetRandomAnyScroll() => _picker.PickAnyScroll();

    public void ResolveStartingScrolls(ClassData classData, List<string> scrollsList)
    {
        if (classData.StartingScrolls == null)
            return;

        foreach (var scrollKey in classData.StartingScrolls)
        {
            if (scrollKey == MorkBorgConstants.ScrollToken.RandomUnclean)
            {
                var scroll = _picker.PickScroll(ScrollKind.Unclean);
                if (scroll is null)
                    throw new InvalidOperationException(
                        $"Class '{classData.Name}' requires an Unclean scroll but no Unclean scrolls exist in the data.");
                scrollsList.Add(scroll.ToFormattedString());
            }
            else if (scrollKey == MorkBorgConstants.ScrollToken.RandomSacred)
            {
                var scroll = _picker.PickScroll(ScrollKind.Sacred);
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
                var sacredScroll = _picker.PickScroll(ScrollKind.Sacred);
                if (sacredScroll is null)
                    throw new InvalidOperationException(
                        "A Sacred scroll is required by starting item data but no Sacred scrolls exist in the data.");
                scrollsList?.Add(sacredScroll.ToFormattedString());
                return true;

            case MorkBorgConstants.ScrollToken.RandomUncleanScroll:
                var uncleanScroll = _picker.PickScroll(ScrollKind.Unclean);
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
