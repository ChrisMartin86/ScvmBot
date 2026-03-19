using ScvmBot.Bot.Games.MorkBorg;
using ScvmBot.Bot.Models.MorkBorg;

namespace ScvmBot.Games.MorkBorg.Tests;

public class VignetteGeneratorTests
{
    [Fact]
    public void Generate_EmptyTemplates_ReturnsEmptyString()
    {
        var data = new VignetteData();
        var generator = new VignetteGenerator(data);
        var character = MakeCharacter();

        var result = generator.Generate(character);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Generate_ReplacesNamePlaceholder()
    {
        var data = MakeMinimalData("{name} walks in.");
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter(name: "Grittr");

        var result = generator.Generate(character);

        Assert.StartsWith("Grittr", result);
    }

    [Fact]
    public void Generate_UsesClassIntro_ForKnownClass()
    {
        var data = MakeMinimalData("{classIntro}");
        data.ClassIntros["Fanged Deserter"] = new List<string> { "a feral beast" };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter(className: "Fanged Deserter");

        var result = generator.Generate(character);

        Assert.Equal("a feral beast", result);
    }

    [Fact]
    public void Generate_UsesClasslessIntro_WhenNoClass()
    {
        var data = MakeMinimalData("{classIntro}");
        data.ClassIntros["Classless"] = new List<string> { "a nameless wretch" };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter(className: null);

        var result = generator.Generate(character);

        Assert.Equal("a nameless wretch", result);
    }

    [Fact]
    public void Generate_FallsBackToDefault_WhenClassNotFound()
    {
        var data = MakeMinimalData("{classIntro}");
        data.ClassIntros["Default"] = new List<string> { "a walking disaster" };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter(className: "Unknown Class");

        var result = generator.Generate(character);

        Assert.Equal("a walking disaster", result);
    }

    [Fact]
    public void Generate_UsesKeyedTrait_FromCharacterDescriptions()
    {
        var data = MakeMinimalData("{trait}");
        data.Traits["Cowardly"] = new List<string> { "brave enough to show up, barely" };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter();
        character.Descriptions.Add("Trait: Cowardly");

        var result = generator.Generate(character);

        Assert.Equal("brave enough to show up, barely", result);
    }

    [Fact]
    public void Generate_UsesKeyedBody_FromCharacterDescriptions()
    {
        var data = MakeMinimalData("{body}");
        data.Bodies["Decaying teeth."] = new List<string> { "smiles like a graveyard" };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter();
        character.Descriptions.Add("Body: Decaying teeth.");

        var result = generator.Generate(character);

        Assert.Equal("smiles like a graveyard", result);
    }

    [Fact]
    public void Generate_UsesKeyedHabit_FromCharacterDescriptions()
    {
        var data = MakeMinimalData("{habit}");
        data.Habits["Pyromaniac"] = new List<string> { "loves fire too much" };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter();
        character.Descriptions.Add("Habit: Pyromaniac");

        var result = generator.Generate(character);

        Assert.Equal("loves fire too much", result);
    }

    [Fact]
    public void Generate_FallsBackToRandomEntry_WhenKeyNotFound()
    {
        var data = MakeMinimalData("{trait}");
        data.Traits["Cowardly"] = new List<string> { "the fallback phrase" };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter();
        character.Descriptions.Add("Trait: Some Unknown Trait");

        var result = generator.Generate(character);

        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void Generate_FullTemplate_ProducesCoherentOutput()
    {
        var data = MakeMinimalData("Meet {name}, {classIntro}. {body} {habit} {closer}");
        data.ClassIntros["Default"] = new List<string> { "a wretch" };
        data.Bodies["Decaying teeth."] = new List<string> { "Teeth leaving one by one." };
        data.Habits["Pyromaniac"] = new List<string> { "Stares at fire." };
        data.Closers = new List<string> { "Good luck." };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter(name: "Grittr", className: "Unknown");
        character.Descriptions.Add("Body: Decaying teeth.");
        character.Descriptions.Add("Habit: Pyromaniac");

        var result = generator.Generate(character);

        Assert.Equal("Meet Grittr, a wretch. Teeth leaving one by one. Stares at fire. Good luck.", result);
    }

    [Fact]
    public void Generate_CapitalizesAfterPeriod_WhenClassIntroFollowsPeriod()
    {
        var data = MakeMinimalData("{name}. {classIntro}. {closer}");
        data.ClassIntros["Default"] = new List<string> { "a walking disaster" };
        data.Closers = new List<string> { "good luck." };
        var generator = new VignetteGenerator(data, new DeterministicRandom(ManyZeros()));
        var character = MakeCharacter(name: "Grittr");

        var result = generator.Generate(character);

        Assert.Equal("Grittr. A walking disaster. Good luck.", result);
    }

    [Fact]
    public async Task GenerateAsync_CharacterHasVignette()
    {
        var refData = new MorkBorgReferenceDataService(
            Path.Combine(TestUtilities.GetBotProjectPath(), "Data", "MorkBorg"));
        await refData.LoadDataAsync();

        var generator = new CharacterGenerator(refData);
        var character = await generator.GenerateAsync();

        Assert.False(string.IsNullOrWhiteSpace(character.Vignette));
    }

    [Fact]
    public async Task GenerateAsync_VignetteContainsCharacterName()
    {
        var refData = new MorkBorgReferenceDataService(
            Path.Combine(TestUtilities.GetBotProjectPath(), "Data", "MorkBorg"));
        await refData.LoadDataAsync();

        var generator = new CharacterGenerator(refData);
        var character = await generator.GenerateAsync(new CharacterGenerationOptions { Name = "Grittr" });

        Assert.Contains("Grittr", character.Vignette);
    }

    private static Character MakeCharacter(string name = "TestScvm", string? className = null)
    {
        return new Character
        {
            Name = name,
            ClassName = className,
            Strength = 0,
            Agility = 0,
            Presence = 0,
            Toughness = 0,
            HitPoints = 4,
            MaxHitPoints = 4,
            Omens = 1,
            Silver = 30
        };
    }

    private static VignetteData MakeMinimalData(string template)
    {
        return new VignetteData
        {
            Templates = new List<string> { template },
            ClassIntros = new Dictionary<string, List<string>>(),
            Bodies = new Dictionary<string, List<string>>(),
            Habits = new Dictionary<string, List<string>>(),
            Items = new Dictionary<string, List<string>>
            {
                ["Default"] = new List<string> { "armed with junk" }
            },
            Traits = new Dictionary<string, List<string>>(),
            Closers = new List<string> { "good luck" }
        };
    }

    private static int[] ManyZeros() => new int[50];
}
