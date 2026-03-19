using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Bot.Tests;

public class ModelDefaultsTests
{
    [Fact]
    public void Character_Defaults_AreInitialized()
    {
        var character = new Character();

        Assert.Equal(string.Empty, character.Name);
        Assert.NotNull(character.Descriptions);
        Assert.NotNull(character.Items);
        Assert.Empty(character.Descriptions);
        Assert.Empty(character.Items);
    }

    [Fact]
    public void CharacterGenerationOptions_Defaults_AreInitialized()
    {
        var options = new CharacterGenerationOptions();

        Assert.False(options.SkipRandomStartingGear);
        Assert.NotNull(options.ForceItemNames);
        Assert.Empty(options.ForceItemNames);
        Assert.Equal(AbilityRollMethod.ThreeD6, options.RollMethod);
    }
}
