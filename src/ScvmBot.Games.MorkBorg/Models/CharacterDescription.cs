namespace ScvmBot.Games.MorkBorg.Models;

public enum DescriptionCategory
{
    Trait,
    Body,
    Habit,
    Container,
    Beast,
    Gear,
    Explosive,
    Poison,
    Elixir,
    Tools,
    Food,
    Water
}

public sealed record CharacterDescription(DescriptionCategory Category, string Text);
