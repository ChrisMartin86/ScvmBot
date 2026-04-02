namespace ScvmBot.Games.CyBorg.Models;

public enum CyBorgDescriptionCategory
{
    Trait,
    Appearance,
    Glitch
}

public class CyBorgDescription
{
    public CyBorgDescriptionCategory Category { get; set; }
    public string Text { get; set; } = string.Empty;

    public CyBorgDescription(CyBorgDescriptionCategory category, string text)
    {
        Category = category;
        Text = text;
    }
}
