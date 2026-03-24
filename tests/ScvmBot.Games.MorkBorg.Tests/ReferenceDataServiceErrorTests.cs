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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "NOT VALID JSON {{{", TestContext.Current.CancellationToken);

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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "null", TestContext.Current.CancellationToken);

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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            // names.json intentionally absent
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);

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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            // weapons.json intentionally absent
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);

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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]", TestContext.Current.CancellationToken);
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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
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
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "NOT VALID JSON {{{", TestContext.Current.CancellationToken);

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
    public async Task PickName_ReturnsUnknown_WhenNamesListIsEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]", TestContext.Current.CancellationToken);
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            var picker = new MorkBorgRandomPicker(service, new Random(42));
            Assert.Equal("Unknown", picker.PickName());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task PickWeapon_ReturnsNull_WhenWeaponsEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]", TestContext.Current.CancellationToken);
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            var picker = new MorkBorgRandomPicker(service, new Random(42));
            Assert.Null(picker.PickWeapon());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task PickArmor_ReturnsNull_WhenArmorEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]", TestContext.Current.CancellationToken);
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            var picker = new MorkBorgRandomPicker(service, new Random(42));
            Assert.Null(picker.PickArmor());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task PickItem_ReturnsNull_WhenItemsEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]", TestContext.Current.CancellationToken);
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            var picker = new MorkBorgRandomPicker(service, new Random(42));
            Assert.Null(picker.PickItem());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task PickClass_ReturnsNull_WhenClassesEmpty()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]", TestContext.Current.CancellationToken);
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            var picker = new MorkBorgRandomPicker(service, new Random(42));
            Assert.Null(picker.PickClass());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task PickScroll_ReturnsNull_WhenNoScrollsOfType()
    {
        var dir = TestUtilities.CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]", TestContext.Current.CancellationToken);
            var service = await MorkBorgReferenceDataService.CreateAsync(dir);
            var picker = new MorkBorgRandomPicker(service, new Random(42));
            Assert.Null(picker.PickScroll(ScrollKind.Sacred));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
