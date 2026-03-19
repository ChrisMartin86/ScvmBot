using ScvmBot.Bot.Games.MorkBorg;

namespace ScvmBot.Games.MorkBorg.Tests;

public class ReferenceDataServiceErrorTests
{
    [Fact]
    public async Task LoadDataAsync_Throws_WhenRequiredFileIsMissing()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            var service = new MorkBorgReferenceDataService(dir);

            await Assert.ThrowsAsync<FileNotFoundException>(() => service.LoadDataAsync());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadDataAsync_Throws_WhenRequiredFileContainsMalformedJson()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "NOT VALID JSON {{{");

            var service = new MorkBorgReferenceDataService(dir);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadDataAsync());
            Assert.Contains("Invalid JSON", ex.Message);
            Assert.Contains("classes.json", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadDataAsync_Throws_WhenRequiredFileDeserializesToNull()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            // "null" is valid JSON but deserializes to null for a reference type
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "null");

            var service = new MorkBorgReferenceDataService(dir);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadDataAsync());
            Assert.Contains("Deserialization returned null", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadDataAsync_ToleratesMissingOptionalFiles()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            // Provide only required files (classes + spells) with valid minimal data
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");

            var service = new MorkBorgReferenceDataService(dir);
            await service.LoadDataAsync();

            Assert.Empty(service.Classes);
            Assert.Empty(service.Scrolls);
            Assert.Empty(service.Names);
            Assert.Empty(service.Weapons);
            Assert.Empty(service.Armor);
            Assert.Empty(service.Items);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadDataAsync_Throws_WhenOptionalFileContainsMalformedJson()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "NOT VALID JSON {{{");

            var service = new MorkBorgReferenceDataService(dir);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadDataAsync());
            Assert.Contains("Malformed JSON", ex.Message);
            Assert.Contains("names.json", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Constructor_WithNullPath_UsesDefaultBasePath()
    {
        var service = new MorkBorgReferenceDataService(null);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithEmptyPath_UsesDefaultBasePath()
    {
        var service = new MorkBorgReferenceDataService("  ");
        Assert.NotNull(service);
    }

    [Fact]
    public void GetRandomName_ReturnsUnknown_WhenNamesEmpty()
    {
        var service = new MorkBorgReferenceDataService(TestUtilities.CreateTempDirectory());
        // Names list is empty by default before LoadDataAsync
        Assert.Equal("Unknown", service.GetRandomName(new Random(42)));
    }

    [Fact]
    public void GetRandomWeapon_ReturnsNull_WhenWeaponsEmpty()
    {
        var service = new MorkBorgReferenceDataService(TestUtilities.CreateTempDirectory());
        Assert.Null(service.GetRandomWeapon(new Random(42)));
    }

    [Fact]
    public void GetRandomArmor_ReturnsNull_WhenArmorEmpty()
    {
        var service = new MorkBorgReferenceDataService(TestUtilities.CreateTempDirectory());
        Assert.Null(service.GetRandomArmor(new Random(42)));
    }

    [Fact]
    public void GetRandomItem_ReturnsNull_WhenItemsEmpty()
    {
        var service = new MorkBorgReferenceDataService(TestUtilities.CreateTempDirectory());
        Assert.Null(service.GetRandomItem(new Random(42)));
    }

    [Fact]
    public void GetRandomClass_ReturnsNull_WhenClassesEmpty()
    {
        var service = new MorkBorgReferenceDataService(TestUtilities.CreateTempDirectory());
        Assert.Null(service.GetRandomClass(new Random(42)));
    }

    [Fact]
    public void GetRandomFromTable_ReturnsEmpty_WhenUnknownTableName()
    {
        var service = new MorkBorgReferenceDataService(TestUtilities.CreateTempDirectory());
        Assert.Equal("", service.GetRandomFromTable("NonexistentTable", new Random(42)));
    }

    [Fact]
    public void GetRandomScroll_ReturnsNull_WhenNoScrollsOfType()
    {
        var service = new MorkBorgReferenceDataService(TestUtilities.CreateTempDirectory());
        Assert.Null(service.GetRandomScroll("Sacred", new Random(42)));
    }
}
