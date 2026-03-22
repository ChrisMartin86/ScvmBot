namespace ScvmBot.Games.MorkBorg.Generation;

/// <summary>String constants used throughout MÖRK BORG character generation.</summary>
public static class MorkBorgConstants
{
    /// <summary>
    /// The <see cref="Models.CharacterGenerationOptions.ClassName"/> sentinel value meaning
    /// "generate a classless character, applying no class".
    /// </summary>
    public const string ClasslessClassName = "none";

    /// <summary>Valid values for <see cref="Reference.ClassData.StartingEquipmentMode"/>.</summary>
    public static class EquipmentMode
    {
        public const string Classless = "classless";
        public const string Ordinary = "ordinary";
        public const string Custom = "custom";
    }

    /// <summary>
    /// Generation token strings accepted in the <c>startingScrolls</c> and
    /// <c>startingItems</c> class data arrays in classes.json.
    /// </summary>
    public static class ScrollToken
    {
        /// <summary>In <c>startingItems</c>: generate a random Sacred scroll.</summary>
        public const string RandomSacredScroll = "random_sacred_scroll";

        /// <summary>In <c>startingItems</c>: generate a random Unclean scroll.</summary>
        public const string RandomUncleanScroll = "random_unclean_scroll";

        /// <summary>In <c>startingScrolls</c> or <c>startingItems</c>: generate a random scroll of either type.</summary>
        public const string RandomAnyScroll = "random_any_scroll";

        /// <summary>In <c>startingScrolls</c>: generate a random Unclean scroll.</summary>
        public const string RandomUnclean = "random_unclean";

        /// <summary>In <c>startingScrolls</c>: generate a random Sacred scroll.</summary>
        public const string RandomSacred = "random_sacred";
    }
}
