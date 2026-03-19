using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Bot.Games.MorkBorg;

public sealed class CharacterGenerator
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly Random _rng;

    public CharacterGenerator(MorkBorgReferenceDataService refData, Random? rng = null)
    {
        _refData = refData;
        _rng = rng ?? Random.Shared;
    }

    public async Task<Character> GenerateAsync(
        CharacterGenerationOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new CharacterGenerationOptions();

        var name = options.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = _refData.GetRandomName(_rng);
        }

        var classData = ResolveClass(options);

        // Official rules: 3d6 for all abilities.
        // For classless characters, the optional heroic rule allows 4d6-drop-lowest for exactly 2 random abilities.
        // Classes always use 3d6.
        var classless = classData == null;
        var useHeroicRoll = classless && options.RollMethod == AbilityRollMethod.FourD6DropLowest;

        var heroicAbilityIndices = new HashSet<int>();
        if (useHeroicRoll)
        {
            while (heroicAbilityIndices.Count < 2)
            {
                heroicAbilityIndices.Add(_rng.Next(4));  // 0=STR, 1=AGI, 2=PRE, 3=TOU
            }
        }

        var str = options.Strength ?? RollAbilityModifier(heroicAbilityIndices.Contains(0) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);
        var agi = options.Agility ?? RollAbilityModifier(heroicAbilityIndices.Contains(1) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);
        var pre = options.Presence ?? RollAbilityModifier(heroicAbilityIndices.Contains(2) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);
        var tou = options.Toughness ?? RollAbilityModifier(heroicAbilityIndices.Contains(3) ? AbilityRollMethod.FourD6DropLowest : AbilityRollMethod.ThreeD6);

        // Apply class stat modifiers only to rolled (non-overridden) abilities
        if (classData != null)
        {
            if (options.Strength == null) str = Math.Clamp(str + classData.StrengthModifier, -3, 3);
            if (options.Agility == null) agi = Math.Clamp(agi + classData.AgilityModifier, -3, 3);
            if (options.Presence == null) pre = Math.Clamp(pre + classData.PresenceModifier, -3, 3);
            if (options.Toughness == null) tou = Math.Clamp(tou + classData.ToughnessModifier, -3, 3);
        }

        // HP = Toughness + hit die, minimum 1
        var hitDieSize = ParseDieSize(classData?.HitDie ?? "d8");
        var maxHp = options.MaxHitPoints ?? Math.Max(1, tou + RollDie(hitDieSize));
        var hp = options.HitPoints ?? maxHp;

        var omenDieSize = ParseDieSize(classData?.OmenDie ?? "d2");
        var omens = options.Omens ?? RollDie(omenDieSize);

        // Use class-specific silver formula if defined, otherwise 2d6 × 10
        var silver = options.Silver
            ?? (classData?.StartingSilver is { Length: > 0 } silverFormula
                ? RollSilver(silverFormula)
                : (RollDie(6) + RollDie(6)) * 10);

        var weaponFormatted = ResolveWeapon(options, classData);

        var armorFormatted = ResolveArmor(options, classData);

        var itemsList = new List<string>();
        var descriptionsList = new List<string>();
        var scrollsList = new List<string>();

        if (classless)
        {
            itemsList.Add("Waterskin (4 days)");
            var foodDays = RollDie(4);
            itemsList.Add($"Dried food ({foodDays} day(s))");

            ApplyStartingContainer(itemsList, descriptionsList, options);
            ApplyStartingEquipmentTables(itemsList, descriptionsList, scrollsList, options, pre);
        }
        else
        {
            var equipMode = classData?.StartingEquipmentMode ?? "ordinary";

            switch (equipMode)
            {
                case "classless":
                    itemsList.Add("Waterskin (4 days)");
                    var classlessFood = RollDie(4);
                    itemsList.Add($"Dried food ({classlessFood} day(s))");
                    ApplyStartingContainer(itemsList, descriptionsList, options);
                    ApplyStartingEquipmentTables(itemsList, descriptionsList, scrollsList, options, pre);
                    break;

                case "ordinary":
                    itemsList.Add("Waterskin (4 days)");
                    var ordinaryFood = RollDie(4);
                    itemsList.Add($"Dried food ({ordinaryFood} day(s))");
                    ApplyStartingContainer(itemsList, descriptionsList, options);
                    ApplyClassStartingItems(itemsList, descriptionsList, classData, scrollsList);
                    break;

                case "custom":
                    // No base kit — only class-defined startingItems
                    ApplyClassStartingItems(itemsList, descriptionsList, classData, scrollsList);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown starting equipment mode '{equipMode}' for class '{classData?.Name ?? "unknown"}'.");
            }
        }

        if (options.ForceItemNames.Count > 0)
        {
            foreach (var itemName in options.ForceItemNames)
                AddItemByName(itemsList, itemName);
        }

        if (classData?.StartingScrolls != null)
        {
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

        descriptionsList.Add($"Trait: {_refData.GetRandomFromTable("Trait", _rng)}");
        descriptionsList.Add($"Body: {_refData.GetRandomFromTable("BrokenBody", _rng)}");
        descriptionsList.Add($"Habit: {_refData.GetRandomFromTable("BadHabit", _rng)}");

        var character = new Character
        {
            Name = name!,
            Strength = str,
            Agility = agi,
            Presence = pre,
            Toughness = tou,
            Omens = omens,
            MaxHitPoints = maxHp,
            HitPoints = hp,
            Silver = silver,
            EquippedWeapon = weaponFormatted,
            EquippedArmor = armorFormatted,
            ClassName = classData?.Name,
            ClassAbility = classData?.ClassAbility,
            Corruption = 0,
            ScrollsKnown = scrollsList,
            Descriptions = descriptionsList,
            Items = itemsList
        };

        var vignetteGenerator = new VignetteGenerator(_refData.Vignettes, _rng);
        character.Vignette = vignetteGenerator.Generate(character);

        return character;
    }

    /// <summary>
    /// ClassName states:
    /// - "none" (case-insensitive) => classless
    /// - null => omitted, rolls random 50/50
    /// - any other string => exact class name lookup
    /// - empty string => classless (backward compat)
    /// </summary>
    private ClassData? ResolveClass(CharacterGenerationOptions options)
    {
        if (string.Equals(options.ClassName, "none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (options.ClassName == null)
        {
            // 50/50 classless vs classed: d6 1-3 = classless, 4-6 = random class
            var roll = RollDie(6);
            return roll <= 3 ? null : _refData.GetRandomClass(_rng);
        }

        // empty string => backward compat, treat as classless
        if (string.IsNullOrWhiteSpace(options.ClassName))
        {
            return null;
        }

        return _refData.GetClassByName(options.ClassName)
            ?? throw new InvalidOperationException($"Unknown class '{options.ClassName}'.");
    }

    private string? ResolveWeapon(CharacterGenerationOptions options, ClassData? classData)
    {
        if (!string.IsNullOrWhiteSpace(options.WeaponName))
        {
            var weapon = _refData.GetWeaponByName(options.WeaponName);
            return weapon?.ToFormattedString();
        }

        if (classData?.StartingWeapons.Count > 0)
        {
            var classWeapon = classData.StartingWeapons[_rng.Next(classData.StartingWeapons.Count)];
            var weapon = _refData.GetWeaponByName(classWeapon);
            if (weapon != null)
                return weapon.ToFormattedString();
        }

        // Roll on the d10 weapon table
        var weaponDieSize = ParseDieSize(classData?.WeaponRollDie ?? "d10");
        var roll = RollDie(weaponDieSize);
        var weaponName = roll switch
        {
            1 => "Femur",
            2 => "Staff",
            3 => "Shortsword",
            4 => "Knife",
            5 => "Warhammer",
            6 => "Sword",
            7 => "Bow",
            8 => "Flail",
            9 => "Crossbow",
            10 => "Zweihänder",
            _ => "Knife"
        };

        var selectedWeapon = _refData.GetWeaponByName(weaponName);
        return selectedWeapon?.ToFormattedString();
    }

    private string? ResolveArmor(CharacterGenerationOptions options, ClassData? classData)
    {
        if (!string.IsNullOrWhiteSpace(options.ArmorName))
        {
            var armor = _refData.GetArmorByName(options.ArmorName);
            return armor?.ToFormattedString();
        }

        if (classData?.StartingArmor.Count > 0)
        {
            var classArmor = classData.StartingArmor[_rng.Next(classData.StartingArmor.Count)];
            var armor = _refData.GetArmorByName(classArmor);
            if (armor != null)
                return armor.ToFormattedString();
        }

        // Roll for armor tier
        var armorDieSize = ParseDieSize(classData?.ArmorRollDie ?? "d4");
        var roll = RollDie(armorDieSize);
        var tier = roll - 1; // d4 roll 1=>tier 0, 2=>1, 3=>2, 4=>3
        var selectedArmor = _refData.GetArmorByTier(tier);
        return selectedArmor?.ToFormattedString();
    }

    private void ApplyStartingContainer(
        List<string> items,
        List<string> descriptions,
        CharacterGenerationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.StartingContainerOverride))
        {
            AddItemByName(items, options.StartingContainerOverride);
            return;
        }

        var roll = RollDie(6);

        switch (roll)
        {
            case 1:
            case 2:
                descriptions.Add("Container: none");
                return;

            case 3:
                AddItemByName(items, "Backpack");
                descriptions.Add("Container: backpack");
                return;

            case 4:
                AddItemByName(items, "Sack");
                descriptions.Add("Container: sack");
                return;

            case 5:
                AddItemByName(items, "Small wagon");
                descriptions.Add("Container: small wagon");
                return;

            case 6:
                AddItemByName(items, "Mule");
                descriptions.Add("Beast: mule");
                return;
        }
    }

    private void ApplyStartingEquipmentTables(
        List<string> items,
        List<string> descriptions,
        List<string> scrolls,
        CharacterGenerationOptions options,
        int presenceModifier)
    {
        if (options.SkipRandomStartingGear)
            return;

        var tableA = options.StartingGearRollA ?? RollDie(12);
        var tableB = options.StartingGearRollB ?? RollDie(12);

        ApplyStartingGearRollA(tableA, items, descriptions, presenceModifier);
        ApplyStartingGearRollB(tableB, items, descriptions, scrolls, presenceModifier);
    }

    /// <summary>Classless d12 Table A: starting gear roll outcomes.</summary>
    private void ApplyStartingGearRollA(
        int roll,
        List<string> items,
        List<string> descriptions,
        int presenceModifier)
    {
        switch (roll)
        {
            case 1:
                AddItemByName(items, "Rope");
                descriptions.Add("Gear: rope (30 feet)");
                break;

            case 2:
                var torches = Math.Max(1, presenceModifier + 4);
                AddItemByName(items, "Torch");
                descriptions.Add($"Gear: torches x{torches}");
                break;

            case 3:
                AddItemByName(items, "Oil lamp");
                AddItemByName(items, "Lantern oil");
                var hours = Math.Max(1, presenceModifier + 6);
                descriptions.Add($"Gear: lantern + oil ({hours} hours)");
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
                descriptions.Add("Gear: metal file and lockpicks");
                break;

            case 7:
                AddItemByName(items, "Bear trap");
                descriptions.Add("Gear: bear trap (d6 damage, DR14 to spot)");
                break;

            case 8:
                AddItemByName(items, "Bomb");
                descriptions.Add("Explosive: bomb (2d6 damage to all nearby)");
                break;

            case 9:
                AddItemByName(items, "Red poison");
                descriptions.Add("Poison: red (d4 uses, ingestion, d6 damage)");
                break;

            case 10:
                AddItemByName(items, "Life elixir");
                descriptions.Add("Elixir: heals d6 HP when consumed");
                break;

            case 11:
                AddItemByName(items, "Heavy chain");
                descriptions.Add("Gear: heavy chain (15 feet, can be used as weapon or restraint)");
                break;

            case 12:
                AddItemByName(items, "Grappling hook");
                descriptions.Add("Gear: grappling hook");
                break;
        }
    }

    /// <summary>Classless d12 Table B: starting gear roll outcomes.</summary>
    private void ApplyStartingGearRollB(
        int roll,
        List<string> items,
        List<string> descriptions,
        List<string> scrolls,
        int presenceModifier)
    {
        switch (roll)
        {
            case 1:
                AddItemByName(items, "Small vicious dog");
                descriptions.Add("Beast: small vicious dog (1 HP, d4 bite damage)");
                break;

            case 2:
                descriptions.Add("Beasts: d4 monkeys (1 HP each, d2 damage)");
                break;

            case 3:
                AddItemByName(items, "Life elixir");
                descriptions.Add("Elixir: heals d6 HP when consumed");
                break;

            case 4:
                AddItemByName(items, "Exquisite perfume");
                descriptions.Add("Gear: exquisite perfume");
                break;

            case 5:
                AddItemByName(items, "Toolbox");
                descriptions.Add("Tools: complete toolbox for repairs");
                break;

            case 6:
                AddItemByName(items, "Heavy chain");
                descriptions.Add("Gear: heavy chain (15 feet, can be used as weapon or restraint)");
                break;

            case 7:
                AddItemByName(items, "Grappling hook");
                descriptions.Add("Gear: grappling hook");
                break;

            case 8:
                AddItemByName(items, "Shield");
                descriptions.Add("Gear: shield (-1 hp damage or break to ignore one attack)");
                break;

            case 9:
                AddItemByName(items, "Crowbar");
                descriptions.Add("Gear: crowbar (can be used as improvised weapon d4)");
                break;

            case 10:
                AddItemByName(items, "Lard");
                descriptions.Add("Gear: lard (may function as 5 meals)");
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
                descriptions.Add("Gear: tent");
                break;
        }
    }

    // -----------------------------
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

    private int RollDie(int sides)
    {
        if (sides <= 0) throw new ArgumentOutOfRangeException(nameof(sides));
        return _rng.Next(1, sides + 1);
    }

    private int RollAbilityModifier(AbilityRollMethod method)
    {
        int total = method == AbilityRollMethod.FourD6DropLowest
            ? RollFourD6DropLowest()
            : RollDie(6) + RollDie(6) + RollDie(6);

        return total switch
        {
            <= 4 => -3,
            <= 6 => -2,
            <= 8 => -1,
            <= 12 => 0,
            <= 14 => 1,
            <= 16 => 2,
            _ => 3
        };
    }

    private int RollFourD6DropLowest()
    {
        var rolls = new[] { RollDie(6), RollDie(6), RollDie(6), RollDie(6) };
        return rolls.Sum() - rolls.Min();
    }

    /// <summary>Parses a die string like "d8" or "d10" and returns the numeric size.</summary>
    internal static int ParseDieSize(string die)
    {
        if (string.IsNullOrWhiteSpace(die)) return 8;
        var numeric = die.TrimStart('d', 'D');
        return int.TryParse(numeric, out var size) && size > 0 ? size : 8;
    }

    private int RollSilver(string formula)
    {
        return formula.ToLowerInvariant() switch
        {
            "d6x10" => RollDie(6) * 10,
            "2d6x10" => (RollDie(6) + RollDie(6)) * 10,
            "d6x10x3" => RollDie(6) * 10 * 3,
            _ => throw new InvalidOperationException($"Unsupported silver formula '{formula}'.")
        };
    }

    private string GetRandomAnyScroll()
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

    private void ApplyClassStartingItems(List<string> items, List<string> descriptions, ClassData? classData, List<string>? scrollsList = null)
    {
        if (classData?.StartingItems == null || classData.StartingItems.Count == 0)
            return;

        foreach (var itemEntry in classData.StartingItems)
        {
            if (string.IsNullOrWhiteSpace(itemEntry))
                continue;

            var trimmed = itemEntry.Trim();

            if (TryProcessStartingItemToken(trimmed, items, scrollsList))
                continue;

            AddItemByName(items, trimmed);
        }
    }

    private bool TryProcessStartingItemToken(string token, List<string> items, List<string>? scrollsList = null)
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

            // If it looks like a token but is unrecognized, fail loudly
            default:
                if (token.StartsWith("random_", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Unsupported generation token in startingItems: '{token}'");
                return false;
        }
    }
}
