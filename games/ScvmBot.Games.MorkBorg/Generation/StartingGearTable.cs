using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Generation;

public sealed class StartingGearTable
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly DiceRoller _dice;
    private readonly ScrollResolver _scrollResolver;
    private readonly Random _rng;

    public StartingGearTable(
        MorkBorgReferenceDataService refData,
        DiceRoller dice,
        ScrollResolver scrollResolver,
        Random rng)
    {
        _refData = refData;
        _dice = dice;
        _scrollResolver = scrollResolver;
        _rng = rng;
    }

    public void ApplyStartingEquipment(
        List<string> items,
        List<CharacterDescription> descriptions,
        List<string> scrolls,
        CharacterGenerationOptions options,
        ClassData? classData,
        bool classless,
        int presenceModifier)
    {
        if (classless)
        {
            AddBaseKit(items);
            ApplyStartingContainer(items, descriptions, options);
            ApplyStartingEquipmentTables(items, descriptions, scrolls, options, presenceModifier);
        }
        else
        {
            var equipMode = classData?.StartingEquipmentMode ?? "ordinary";

            switch (equipMode)
            {
                case "classless":
                    AddBaseKit(items);
                    ApplyStartingContainer(items, descriptions, options);
                    ApplyStartingEquipmentTables(items, descriptions, scrolls, options, presenceModifier);
                    break;

                case "ordinary":
                    AddBaseKit(items);
                    ApplyStartingContainer(items, descriptions, options);
                    ApplyClassStartingItems(items, descriptions, classData, scrolls);
                    break;

                case "custom":
                    ApplyClassStartingItems(items, descriptions, classData, scrolls);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown starting equipment mode '{equipMode}' for class '{classData?.Name ?? "unknown"}'.");
            }
        }

        if (options.ForceItemNames.Count > 0)
        {
            foreach (var itemName in options.ForceItemNames)
                AddItemByName(items, itemName);
        }
    }

    private void AddBaseKit(List<string> items)
    {
        items.Add("Waterskin (4 days)");
        var foodDays = _dice.RollDie(4);
        items.Add($"Dried food ({foodDays} day(s))");
    }

    internal void ApplyStartingContainer(
        List<string> items,
        List<CharacterDescription> descriptions,
        CharacterGenerationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.StartingContainerOverride))
        {
            AddItemByName(items, options.StartingContainerOverride);
            return;
        }

        var roll = _dice.RollDie(6);

        switch (roll)
        {
            case 1:
            case 2:
                descriptions.Add(new CharacterDescription(DescriptionCategory.Container, "none"));
                return;

            case 3:
                AddItemByName(items, "Backpack");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Container, "backpack"));
                return;

            case 4:
                AddItemByName(items, "Sack");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Container, "sack"));
                return;

            case 5:
                AddItemByName(items, "Small wagon");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Container, "small wagon"));
                return;

            case 6:
                AddItemByName(items, "Mule");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Beast, "mule"));
                return;
        }
    }

    private void ApplyStartingEquipmentTables(
        List<string> items,
        List<CharacterDescription> descriptions,
        List<string> scrolls,
        CharacterGenerationOptions options,
        int presenceModifier)
    {
        if (options.SkipRandomStartingGear)
            return;

        var tableA = options.StartingGearRollA ?? _dice.RollDie(12);
        var tableB = options.StartingGearRollB ?? _dice.RollDie(12);

        ApplyGearRollA(tableA, items, descriptions, presenceModifier);
        ApplyGearRollB(tableB, items, descriptions, scrolls, presenceModifier);
    }

    /// <summary>Classless d12 Table A: starting gear roll outcomes.</summary>
    internal void ApplyGearRollA(
        int roll,
        List<string> items,
        List<CharacterDescription> descriptions,
        int presenceModifier)
    {
        switch (roll)
        {
            case 1:
                AddItemByName(items, "Rope");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "rope (30 feet)"));
                break;

            case 2:
                var torches = Math.Max(1, presenceModifier + 4);
                AddItemByName(items, "Torch");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, $"torches x{torches}"));
                break;

            case 3:
                AddItemByName(items, "Oil lamp");
                AddItemByName(items, "Lantern oil");
                var hours = Math.Max(1, presenceModifier + 6);
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, $"lantern + oil ({hours} hours)"));
                break;

            case 4:
                AddItemByName(items, "Magnesium strip");
                break;

            case 5:
                AddItemByName(items, "Medicine chest");
                break;

            case 6:
                AddItemByName(items, "Metal file");
                AddItemByName(items, "Lockpicks");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "metal file and lockpicks"));
                break;

            case 7:
                AddItemByName(items, "Bear trap");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "bear trap (d6 damage, DR14 to spot)"));
                break;

            case 8:
                AddItemByName(items, "Bomb");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Explosive, "bomb (2d6 damage to all nearby)"));
                break;

            case 9:
                AddItemByName(items, "Red poison");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Poison, "red (d4 uses, ingestion, d6 damage)"));
                break;

            case 10:
                AddItemByName(items, "Life elixir");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Elixir, "heals d6 HP when consumed"));
                break;

            case 11:
                AddItemByName(items, "Heavy chain");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "heavy chain (15 feet, can be used as weapon or restraint)"));
                break;

            case 12:
                AddItemByName(items, "Grappling hook");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "grappling hook"));
                break;
        }
    }

    /// <summary>Classless d12 Table B: starting gear roll outcomes.</summary>
    internal void ApplyGearRollB(
        int roll,
        List<string> items,
        List<CharacterDescription> descriptions,
        List<string> scrolls,
        int presenceModifier)
    {
        switch (roll)
        {
            case 1:
                AddItemByName(items, "Small vicious dog");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Beast, "small vicious dog (1 HP, d4 bite damage)"));
                break;

            case 2:
                descriptions.Add(new CharacterDescription(DescriptionCategory.Beast, "d4 monkeys (1 HP each, d2 damage)"));
                break;

            case 3:
                AddItemByName(items, "Life elixir");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Elixir, "heals d6 HP when consumed"));
                break;

            case 4:
                AddItemByName(items, "Exquisite perfume");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "exquisite perfume"));
                break;

            case 5:
                AddItemByName(items, "Toolbox");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Tools, "complete toolbox for repairs"));
                break;

            case 6:
                AddItemByName(items, "Heavy chain");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "heavy chain (15 feet, can be used as weapon or restraint)"));
                break;

            case 7:
                AddItemByName(items, "Grappling hook");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "grappling hook"));
                break;

            case 8:
                AddItemByName(items, "Shield");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "shield (-1 hp damage or break to ignore one attack)"));
                break;

            case 9:
                AddItemByName(items, "Crowbar");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "crowbar (can be used as improvised weapon d4)"));
                break;

            case 10:
                AddItemByName(items, "Lard");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "lard (may function as 5 meals)"));
                break;

            case 11:
                var scroll = _refData.GetRandomScroll("Sacred", _rng);
                if (scroll != null)
                {
                    scrolls.Add(scroll.ToFormattedString());
                }
                break;

            case 12:
                AddItemByName(items, "Tent");
                descriptions.Add(new CharacterDescription(DescriptionCategory.Gear, "tent"));
                break;
        }
    }

    private void ApplyClassStartingItems(
        List<string> items,
        List<CharacterDescription> descriptions,
        ClassData? classData,
        List<string>? scrollsList = null)
    {
        if (classData?.StartingItems == null || classData.StartingItems.Count == 0)
            return;

        foreach (var itemEntry in classData.StartingItems)
        {
            if (string.IsNullOrWhiteSpace(itemEntry))
                continue;

            var trimmed = itemEntry.Trim();

            if (_scrollResolver.TryProcessStartingItemToken(trimmed, scrollsList))
                continue;

            AddItemByName(items, trimmed);
        }
    }

    private void AddItemByName(List<string> items, string itemName)
    {
        var item = _refData.GetItemByName(itemName);
        if (item != null)
        {
            var formatted = item.ToFormattedString();
            if (!items.Contains(formatted))
                items.Add(formatted);
        }
    }
}
