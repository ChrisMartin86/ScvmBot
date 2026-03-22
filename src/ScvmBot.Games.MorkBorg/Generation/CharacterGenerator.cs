using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Generation;

public sealed class CharacterGenerator
{
    private readonly MorkBorgReferenceDataService _refData;
    private readonly Random _rng;
    private readonly DiceRoller _dice;
    private readonly AbilityRoller _abilityRoller;
    private readonly WeaponResolver _weaponResolver;
    private readonly ArmorResolver _armorResolver;
    private readonly ScrollResolver _scrollResolver;
    private readonly StartingGearTable _startingGearTable;

    public CharacterGenerator(MorkBorgReferenceDataService refData, Random? rng = null)
    {
        _refData = refData;
        _rng = rng ?? Random.Shared;
        _dice = new DiceRoller(_rng);
        _abilityRoller = new AbilityRoller(_dice, _rng);
        _weaponResolver = new WeaponResolver(refData, _dice, _rng);
        _armorResolver = new ArmorResolver(refData, _dice, _rng);
        _scrollResolver = new ScrollResolver(refData, _rng);
        _startingGearTable = new StartingGearTable(refData, _dice, _scrollResolver, _rng);
    }

    public Character Generate(
        CharacterGenerationOptions? options = null)
    {
        options ??= new CharacterGenerationOptions();

        var name = options.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = _refData.GetRandomName(_rng);
        }

        var classData = ResolveClass(options);
        var classless = classData == null;

        var abilities = _abilityRoller.Roll(options, classData);

        // HP = Toughness + hit die, minimum 1
        var hitDieSize = DiceRoller.ParseDieSize(classData?.HitDie ?? "d8");
        var maxHp = options.MaxHitPoints ?? Math.Max(1, abilities.Toughness + _dice.RollDie(hitDieSize));
        var hp = options.HitPoints ?? maxHp;

        var omenDieSize = DiceRoller.ParseDieSize(classData?.OmenDie ?? "d2");
        var omens = options.Omens ?? _dice.RollDie(omenDieSize);

        // Use class-specific silver formula if defined, otherwise 2d6 × 10
        var silver = options.Silver
            ?? (classData?.StartingSilver is { Length: > 0 } silverFormula
                ? _dice.RollSilver(silverFormula)
                : (_dice.RollDie(6) + _dice.RollDie(6)) * 10);

        ValidateOverrides(maxHp, hp, omens, silver);

        var weaponFormatted = _weaponResolver.Resolve(options, classData);
        var armorFormatted = _armorResolver.Resolve(options, classData);

        var itemsList = new List<string>();
        var descriptionsList = new List<CharacterDescription>();
        var scrollsList = new List<string>();

        _startingGearTable.ApplyStartingEquipment(
            itemsList, descriptionsList, scrollsList,
            options, classData, classless, abilities.Presence);

        if (classData != null)
        {
            _scrollResolver.ResolveStartingScrolls(classData, scrollsList);
        }

        descriptionsList.Add(new CharacterDescription(DescriptionCategory.Trait, _refData.GetRandomTrait(_rng)));
        descriptionsList.Add(new CharacterDescription(DescriptionCategory.Body, _refData.GetRandomBody(_rng)));
        descriptionsList.Add(new CharacterDescription(DescriptionCategory.Habit, _refData.GetRandomHabit(_rng)));

        var character = new Character
        {
            Name = name!,
            Strength = abilities.Strength,
            Agility = abilities.Agility,
            Presence = abilities.Presence,
            Toughness = abilities.Toughness,
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
        if (string.Equals(options.ClassName, MorkBorgConstants.ClasslessClassName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (options.ClassName == null)
        {
            // 50/50 classless vs classed: d6 1-3 = classless, 4-6 = random class
            var roll = _dice.RollDie(6);
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

    private static void ValidateOverrides(int maxHp, int hp, int omens, int silver)
    {
        if (maxHp < 1)
            throw new ArgumentException($"MaxHitPoints must be at least 1 (was {maxHp}).");
        if (hp < 1)
            throw new ArgumentException($"HitPoints must be at least 1 (was {hp}).");
        if (hp > maxHp)
            throw new ArgumentException($"HitPoints ({hp}) cannot exceed MaxHitPoints ({maxHp}).");
        if (omens < 0)
            throw new ArgumentException($"Omens must be non-negative (was {omens}).");
        if (silver < 0)
            throw new ArgumentException($"Silver must be non-negative (was {silver}).");
    }

    /// <summary>Backward-compatible static helper for die string parsing.</summary>
    internal static int ParseDieSize(string die) => DiceRoller.ParseDieSize(die);
}
