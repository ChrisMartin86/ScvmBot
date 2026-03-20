using Discord;
using ScvmBot.Bot.Games.MorkBorg;
using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Bot.Tests;

public class CharacterCardBuilderTests
{
    [Fact]
    public void Build_ReturnsEmbed_WithCharacterNameAsTitle()
    {
        var character = new Character
        {
            Name = "Karg the Doomed",
            Strength = 2,
            Agility = -1,
            Presence = 0,
            Toughness = 1,
            HitPoints = 6,
            Omens = 2,
            Silver = 30,
            EquippedWeapon = "Femur (d6)",
            EquippedArmor = "No Armor"
        };

        var embed = CharacterCardBuilder.Build(character);

        Assert.Equal(EmbedType.Rich, embed.Type);
        Assert.Equal("Karg the Doomed", embed.Title);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithSummaryDescription()
    {
        var character = new Character
        {
            Name = "Grot",
            ClassName = "Fanged Deserter",
            HitPoints = 8,
            Omens = 4,
            Silver = 20,
            EquippedWeapon = "Sword (d6)",
            EquippedArmor = "Leather"
        };

        var embed = CharacterCardBuilder.Build(character);

        Assert.Equal("Fanged Deserter — HP 8 | Omens 4 | 20s", embed.Description);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithNoClassInSummary_WhenClassNameIsEmpty()
    {
        var character = new Character
        {
            Name = "Classless",
            ClassName = "",
            HitPoints = 4,
            Omens = 1,
            Silver = 10,
            EquippedWeapon = "Knife (d4)",
            EquippedArmor = "No Armor"
        };

        var embed = CharacterCardBuilder.Build(character);

        Assert.StartsWith("No Class", embed.Description);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithNoClassInSummary_WhenClassNameIsNull()
    {
        var character = new Character
        {
            Name = "Nullclass",
            HitPoints = 5,
            Omens = 2,
            Silver = 40,
            EquippedWeapon = "Staff (d4)",
            EquippedArmor = "No Armor"
        };

        var embed = CharacterCardBuilder.Build(character);

        Assert.StartsWith("No Class", embed.Description);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithAbilitiesField()
    {
        var character = new Character
        {
            Name = "Able",
            Strength = 2,
            Agility = -1,
            Presence = 0,
            Toughness = 3,
            HitPoints = 8,
            EquippedWeapon = "Sword (d6)",
            EquippedArmor = "No Armor"
        };

        var embed = CharacterCardBuilder.Build(character);

        var field = embed.Fields.First(f => f.Name == "Abilities");
        Assert.Equal("STR +2 · AGI -1 · PRE 0 · TGH +3", field.Value);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithEquipmentField()
    {
        var character = new Character
        {
            Name = "Geared",
            HitPoints = 4,
            EquippedWeapon = "Femur (d6)",
            EquippedArmor = "Leather (tier 1)",
            Items = new List<string> { "Rope", "Torches (3)" }
        };

        var embed = CharacterCardBuilder.Build(character);

        var field = embed.Fields.First(f => f.Name == "Equipment");
        Assert.Contains("Femur (d6)", field.Value);
        Assert.Contains("Leather (tier 1)", field.Value);
        Assert.Contains("Rope", field.Value);
        Assert.Contains("Torches (3)", field.Value);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithDescriptionField()
    {
        var character = new Character
        {
            Name = "Described",
            HitPoints = 3,
            EquippedWeapon = "Knife (d4)",
            EquippedArmor = "No Armor",
            Descriptions = new List<CharacterDescription>
            {
                new(DescriptionCategory.Food, "2 day(s)"),
                new(DescriptionCategory.Trait, "Endlessly Cursed"),
                new(DescriptionCategory.Body, "Rotting Teeth"),
                new(DescriptionCategory.Habit, "Picks Nails Obsessively")
            }
        };

        var embed = CharacterCardBuilder.Build(character);

        var field = embed.Fields.First(f => f.Name == "Description");
        Assert.Contains("Trait: Endlessly Cursed", field.Value);
        Assert.Contains("Body: Rotting Teeth", field.Value);
        Assert.Contains("Habit: Picks Nails Obsessively", field.Value);
        Assert.DoesNotContain("Food:", field.Value);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithScrollsField_WhenScrollsExist()
    {
        var character = new Character
        {
            Name = "Mystic",
            HitPoints = 3,
            EquippedWeapon = "Staff (d4)",
            EquippedArmor = "No Armor",
            ScrollsKnown = new List<string> { "Daemon of the Pit", "Grace of a Dead Saint" }
        };

        var embed = CharacterCardBuilder.Build(character);

        var field = embed.Fields.First(f => f.Name == "Scrolls");
        Assert.Contains("Daemon of the Pit", field.Value);
        Assert.Contains("Grace of a Dead Saint", field.Value);
    }

    [Fact]
    public void Build_ReturnsEmbed_WithoutScrollsField_WhenNoScrolls()
    {
        var character = new Character
        {
            Name = "Mundane",
            HitPoints = 6,
            EquippedWeapon = "Sword (d8)",
            EquippedArmor = "No Armor",
            ScrollsKnown = new List<string>()
        };

        var embed = CharacterCardBuilder.Build(character);

        Assert.DoesNotContain(embed.Fields, f => f.Name == "Scrolls");
    }

    [Fact]
    public void Build_FieldOrder_IsConsistent()
    {
        var character = new Character
        {
            Name = "Orderly",
            Strength = 1,
            Agility = 0,
            Presence = -1,
            Toughness = 2,
            HitPoints = 7,
            Omens = 3,
            Silver = 50,
            ClassName = "Fanged Deserter",
            EquippedWeapon = "Sword (d6)",
            EquippedArmor = "Leather (tier 1)",
            Items = new List<string> { "Rope" },
            Descriptions = new List<CharacterDescription>
            {
                new(DescriptionCategory.Trait, "Bold"),
                new(DescriptionCategory.Body, "Scarred"),
                new(DescriptionCategory.Habit, "Spits")
            },
            ScrollsKnown = new List<string> { "Fireball" }
        };

        var embed = CharacterCardBuilder.Build(character);

        Assert.Equal("Abilities", embed.Fields[0].Name);
        Assert.Equal("Equipment", embed.Fields[1].Name);
        Assert.Equal("Description", embed.Fields[2].Name);
        Assert.Equal("Scrolls", embed.Fields[3].Name);
    }
}
