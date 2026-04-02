using System.Text.Json;

namespace ScvmBot.Games.CyBorg.Reference;

public class CyBorgReferenceDataService
{
    private readonly string _dataRootPath;

    private List<string> _names = new();
    private List<CyBorgWeaponData> _weapons = new();
    private List<CyBorgArmorData> _armor = new();
    private List<CyBorgGearData> _gear = new();
    private List<CyBorgClassData> _classes = new();
    private List<CyBorgAppData> _apps = new();
    private CyBorgDescriptionTables _descriptions = new();

    public IReadOnlyList<string> Names => _names.AsReadOnly();
    public IReadOnlyList<CyBorgWeaponData> Weapons => _weapons.AsReadOnly();
    public IReadOnlyList<CyBorgArmorData> Armor => _armor.AsReadOnly();
    public IReadOnlyList<CyBorgGearData> Gear => _gear.AsReadOnly();
    public IReadOnlyList<CyBorgClassData> Classes => _classes.AsReadOnly();
    public IReadOnlyList<CyBorgAppData> Apps => _apps.AsReadOnly();
    public CyBorgDescriptionTables Descriptions => _descriptions;

    /// <summary>
    /// Creates a fully-initialized service. Throws if required data files are missing or malformed.
    /// This is the only way to obtain an instance; construction and loading are atomic.
    /// </summary>
    public static async Task<CyBorgReferenceDataService> CreateAsync(string? dataRootPath = null)
    {
        var service = new CyBorgReferenceDataService(dataRootPath);
        await service.LoadDataAsync();
        return service;
    }

    /// <summary>
    /// Uses AppContext.BaseDirectory if no path provided, which works across
    /// dev, tests, publish, Docker, and hosted environments.
    /// </summary>
    private CyBorgReferenceDataService(string? dataRootPath = null)
    {
        if (!string.IsNullOrWhiteSpace(dataRootPath))
        {
            _dataRootPath = dataRootPath;
        }
        else
        {
            var baseDir = AppContext.BaseDirectory;
            _dataRootPath = Path.Combine(baseDir, "Data", "CyBorg");
        }
    }

    private async Task LoadDataAsync()
    {
        // Required gameplay datasets — missing or malformed stops startup immediately.
        _classes = await LoadJsonAsync<List<CyBorgClassData>>(Path.Combine(_dataRootPath, "classes.json"));
        _names = await LoadJsonAsync<List<string>>(Path.Combine(_dataRootPath, "names.json"));
        _weapons = await LoadJsonAsync<List<CyBorgWeaponData>>(Path.Combine(_dataRootPath, "weapons.json"));
        _armor = await LoadJsonAsync<List<CyBorgArmorData>>(Path.Combine(_dataRootPath, "armor.json"));
        _gear = await LoadJsonAsync<List<CyBorgGearData>>(Path.Combine(_dataRootPath, "gear.json"));
        _apps = await LoadJsonAsync<List<CyBorgAppData>>(Path.Combine(_dataRootPath, "apps.json"));
        _descriptions = await LoadJsonAsync<CyBorgDescriptionTables>(Path.Combine(_dataRootPath, "descriptions.json"));

        ValidateCreditsFormulas();
    }

    private void ValidateCreditsFormulas()
    {
        foreach (var cls in _classes)
        {
            if (cls.StartingCredits is not { } f) continue;
            if (f.DiceCount <= 0 || f.DiceSides <= 0 || f.Multiplier <= 0)
                throw new InvalidOperationException(
                    $"Class '{cls.Name}' has an invalid startingCredits formula: " +
                    $"diceCount={f.DiceCount}, diceSides={f.DiceSides}, multiplier={f.Multiplier}. " +
                    $"All values must be positive integers.");
        }
    }

    private async Task<T> LoadJsonAsync<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Required data file was not found: {filePath}", filePath);
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null)
            {
                throw new InvalidOperationException($"Deserialization returned null for required data file: {filePath}");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in required data file '{filePath}': {ex.Message}", ex);
        }
    }

    public CyBorgWeaponData? GetWeaponByTableIndex(int tableIndex)
    {
        return _weapons.FirstOrDefault(w => w.TableIndex == tableIndex);
    }

    public CyBorgWeaponData? GetWeaponByName(string name)
    {
        return _weapons.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public CyBorgArmorData? GetArmorByTier(int tier)
    {
        return _armor.FirstOrDefault(a => a.Tier == tier);
    }

    public CyBorgArmorData? GetArmorByName(string name)
    {
        return _armor.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public CyBorgClassData? GetClassByName(string name)
    {
        return _classes.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
