using ScvmBot.Bot.Models.MorkBorg;
using ScvmBot.Bot.Services.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class CharacterSheetMapperTests
{
    private static Character SampleCharacter() => new()
    {
        Name = "Karg",
        Strength = 2,
        Agility = -1,
        Presence = 0,
        Toughness = 1,
        HitPoints = 5,
        MaxHitPoints = 7,
        Omens = 2,
        Silver = 60,
        EquippedWeapon = "Sword (Damage: d8)",
        EquippedArmor = "Light armor (Tier 1, DR: -d2)",
        Items = new List<string> { "Rope", "Torch", "Waterskin", "Dried food" },
        Descriptions = new List<string> { "Trait: stubborn", "Body: scar on face" }
    };

    [Fact]
    public void Map_CopiesBasicStats()
    {
        var data = CharacterSheetMapper.Map(SampleCharacter());

        Assert.Equal("Karg", data.Name);
        Assert.Equal("5", data.HP_Current);
        Assert.Equal("7", data.HP_Max);
        Assert.Equal("2", data.Omens);
        Assert.Equal("60", data.Silver);
    }

    [Theory]
    [InlineData(2, "+2")]
    [InlineData(0, "+0")]
    [InlineData(-1, "-1")]
    public void Map_FormatsAbilityModifiersWithSign(int raw, string expected)
    {
        var ch = SampleCharacter();
        ch.Strength = raw;
        var data = CharacterSheetMapper.Map(ch);
        Assert.Equal(expected, data.Strength);
    }

    [Fact]
    public void Map_ParsesArmorTier_FromFormattedString()
    {
        var ch = SampleCharacter();
        ch.EquippedArmor = "Medium armor (Tier 2, DR: -d4, Defense +2 DR)";
        var data = CharacterSheetMapper.Map(ch);
        Assert.Equal(2, data.ArmorTier);
    }

    [Fact]
    public void Map_ArmorTier_ZeroWhenNoArmor()
    {
        var ch = SampleCharacter();
        ch.EquippedArmor = null;
        var data = CharacterSheetMapper.Map(ch);
        Assert.Equal(0, data.ArmorTier);
        Assert.Equal(string.Empty, data.ArmorText);
    }

    [Fact]
    public void Map_ArmorTier_ZeroWhenNoTierInString()
    {
        var ch = SampleCharacter();
        ch.EquippedArmor = "No armor";
        var data = CharacterSheetMapper.Map(ch);
        Assert.Equal(0, data.ArmorTier);
    }

    [Fact]
    public void Map_EquippedWeapon_GoesIntoWeaponSlotZero()
    {
        var data = CharacterSheetMapper.Map(SampleCharacter());
        Assert.Equal("Sword (Damage: d8)", data.Weapons[0]);
        Assert.Equal(string.Empty, data.Weapons[1]);
    }

    [Fact]
    public void Map_ItemsFillEquipmentGrid_RowByRow()
    {
        var data = CharacterSheetMapper.Map(SampleCharacter());

        // 4 items fill first four slots
        Assert.Equal("Rope", data.Equipment[0]);
        Assert.Equal("Torch", data.Equipment[1]);
        Assert.Equal("Waterskin", data.Equipment[2]);
        Assert.Equal("Dried food", data.Equipment[3]);

        // Remaining slots empty
        Assert.Equal(string.Empty, data.Equipment[4]);
        Assert.Equal(string.Empty, data.Equipment[14]);
    }

    [Fact]
    public void Map_DescriptionsJoinedWithNewlines()
    {
        var data = CharacterSheetMapper.Map(SampleCharacter());
        Assert.Contains("Trait: stubborn", data.Description);
        Assert.Contains("Body: scar on face", data.Description);
    }

    [Fact]
    public void Map_ExcludesFoodAndWaterFromDescription()
    {
        var ch = SampleCharacter();
        ch.Descriptions.Add("Food: 3 day(s)");
        ch.Descriptions.Add("Water: waterskin (4 days capacity)");
        ch.Descriptions.Add("Gear: rope (30 feet)");
        ch.Descriptions.Add("Beast: small vicious dog (1 HP, d4 bite damage)");
        var data = CharacterSheetMapper.Map(ch);
        Assert.DoesNotContain("Food:", data.Description);
        Assert.DoesNotContain("Water:", data.Description);
        Assert.DoesNotContain("Gear:", data.Description);
        Assert.DoesNotContain("Beast:", data.Description);
    }

    [Fact]
    public void Map_PowersAreAllEmpty()
    {
        var data = CharacterSheetMapper.Map(SampleCharacter());
        Assert.All(data.Powers, p => Assert.Equal(string.Empty, p));
    }
}
