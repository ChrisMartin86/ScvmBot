using System.Text.Json;

namespace ScvmBot.Games.MorkBorg.Reference;

public class MorkBorgReferenceDataService
{
    private readonly string _dataRootPath;

    private List<string> _names = new();
    private List<WeaponData> _weapons = new();
    private List<ArmorData> _armor = new();
    private List<ItemData> _items = new();
    private List<ClassData> _classes = new();
    private List<ScrollData> _scrolls = new();
    private DescriptionTables _descriptions = new();
    private VignetteData _vignettes = new();

    public IReadOnlyList<string> Names => _names.AsReadOnly();
    public IReadOnlyList<WeaponData> Weapons => _weapons.AsReadOnly();
    public IReadOnlyList<ArmorData> Armor => _armor.AsReadOnly();
    public IReadOnlyList<ItemData> Items => _items.AsReadOnly();
    public IReadOnlyList<ClassData> Classes => _classes.AsReadOnly();
    public IReadOnlyList<ScrollData> Scrolls => _scrolls.AsReadOnly();
    public DescriptionTables Descriptions => _descriptions;
    public VignetteData Vignettes => _vignettes;

    /// <summary>
    /// Creates a fully-initialized service. Throws if required data files are missing or malformed.
    /// This is the only way to obtain an instance; construction and loading are atomic.
    /// </summary>
    public static async Task<MorkBorgReferenceDataService> CreateAsync(string? dataRootPath = null)
    {
        var service = new MorkBorgReferenceDataService(dataRootPath);
        await service.LoadDataAsync();
        return service;
    }

    /// <summary>
    /// Uses AppContext.BaseDirectory if no path provided, which works across
    /// dev, tests, publish, Docker, and hosted environments.
    /// </summary>
    private MorkBorgReferenceDataService(string? dataRootPath = null)
    {
        if (!string.IsNullOrWhiteSpace(dataRootPath))
        {
            _dataRootPath = dataRootPath;
        }
        else
        {
            // Use AppContext.BaseDirectory which is consistent across all execution contexts
            var baseDir = AppContext.BaseDirectory;
            _dataRootPath = Path.Combine(baseDir, "Data", "MorkBorg");
        }
    }

    private async Task LoadDataAsync()
    {
        // Required gameplay datasets — missing or malformed stops startup immediately.
        _classes = await LoadJsonAsync<List<ClassData>>(Path.Combine(_dataRootPath, "classes.json"));
        _scrolls = await LoadJsonAsync<List<ScrollData>>(Path.Combine(_dataRootPath, "spells.json"));
        _names = await LoadJsonAsync<List<string>>(Path.Combine(_dataRootPath, "names.json"));
        _weapons = await LoadJsonAsync<List<WeaponData>>(Path.Combine(_dataRootPath, "weapons.json"));
        _armor = await LoadJsonAsync<List<ArmorData>>(Path.Combine(_dataRootPath, "armor.json"));
        _items = await LoadJsonAsync<List<ItemData>>(Path.Combine(_dataRootPath, "items.json"));

        // Supplementary datasets — missing file is tolerated; malformed JSON still throws.
        _descriptions = await LoadJsonOptionalAsync<DescriptionTables>(Path.Combine(_dataRootPath, "descriptions.json")) ?? new();
        _vignettes = await LoadJsonOptionalAsync<VignetteData>(Path.Combine(_dataRootPath, "vignettes.json")) ?? new();

        ValidateSilverFormulas();
    }

    private void ValidateSilverFormulas()
    {
        foreach (var cls in _classes)
        {
            if (cls.StartingSilver is not { } f) continue;
            if (f.DiceCount <= 0 || f.DiceSides <= 0 || f.Multiplier <= 0)
                throw new InvalidOperationException(
                    $"Class '{cls.Name}' has an invalid startingSilver formula: " +
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

    /// <summary>
    /// Missing file => null (optional). Malformed JSON => throws (corrupt data file).
    /// </summary>
    private async Task<T?> LoadJsonOptionalAsync<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException jsonEx)
        {
            throw new InvalidOperationException(
                $"Malformed JSON in optional data file '{filePath}': {jsonEx.Message}", jsonEx);
        }
        catch (IOException ioEx) when (ioEx is FileNotFoundException)
        {
            // Race condition: file deleted between existence check and open
            return null;
        }
        catch (IOException ioEx)
        {
            throw new InvalidOperationException(
                $"Error reading data file '{filePath}': {ioEx.Message}", ioEx);
        }
    }



    public WeaponData? GetWeaponByTableIndex(int tableIndex)
    {
        return _weapons.FirstOrDefault(w => w.TableIndex == tableIndex);
    }

    public WeaponData? GetWeaponByName(string name)
    {
        return _weapons.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public ArmorData? GetArmorByTier(int tier)
    {
        return _armor.FirstOrDefault(a => a.Tier == tier);
    }

    public ArmorData? GetArmorByName(string name)
    {
        return _armor.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public ItemData? GetItemByName(string name)
    {
        return _items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public ClassData? GetClassByName(string name)
    {
        return _classes.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

}
