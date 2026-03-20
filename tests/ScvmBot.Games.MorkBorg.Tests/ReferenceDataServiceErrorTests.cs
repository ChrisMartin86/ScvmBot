using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Tests;

public class ReferenceDataServiceErrorTests
{
    [Fact]
    public async Task CreateAsync_Throws_WhenRequiredFileIsMissing()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await Assert.ThrowsAsync<FileNotFoundException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenRequiredFileContainsMalformedJson()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "NOT VALID JSON {{{");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
            Assert.Contains("Invalid JSON", ex.Message);
            Assert.Contains("classes.json", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenRequiredFileDeserializesToNull()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            // "null" is valid JSON but deserializes to null for a reference type
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "null");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
            Assert.Contains("Deserialization returned null", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNamesFileIsMissing()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            // names.json intentionally absent
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");

            await Assert.ThrowsAsync<FileNotFoundException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenWeaponsFileIsMissing()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            // weapons.json intentionally absent
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");

            await Assert.ThrowsAsync<FileNotFoundException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenArmorFileIsMissing()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            // armor.json intentionally absent

            await Assert.ThrowsAsync<FileNotFoundException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CreateAsync_ToleratesMissingOptionalFiles()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            // Six required files: classes, spells, names, weapons, armor, items.
            // Only descriptions.json and vignettes.json are optional.
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
            // descriptions.json and vignettes.json intentionally absent

            var service = await MorkBorgReferenceDataService.CreateAsync(dir);

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
    public async Task CreateAsync_Throws_WhenItemsFileIsMissing()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            // items.json intentionally absent

            await Assert.ThrowsAsync<FileNotFoundException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenItemsFileContainsMalformedJson()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "NOT VALID JSON {{{");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => MorkBorgReferenceDataService.CreateAsync(dir));
            Assert.Contains("Invalid JSON", ex.Message);
            Assert.Contains("items.json", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task GetRandomName_ReturnsUnknown_WhenNamesListIsEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            Assert.Equal("Unknown", service.GetRandomName(new Random(42)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task GetRandomWeapon_ReturnsNull_WhenWeaponsEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            Assert.Null(service.GetRandomWeapon(new Random(42)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task GetRandomArmor_ReturnsNull_WhenArmorEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            Assert.Null(service.GetRandomArmor(new Random(42)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task GetRandomItem_ReturnsNull_WhenItemsEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            Assert.Null(service.GetRandomItem(new Random(42)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task GetRandomClass_ReturnsNull_WhenClassesEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            Assert.Null(service.GetRandomClass(new Random(42)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task GetRandomScroll_ReturnsNull_WhenNoScrollsOfType()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            Assert.Null(service.GetRandomScroll(ScrollKind.Sacred, new Random(42)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
