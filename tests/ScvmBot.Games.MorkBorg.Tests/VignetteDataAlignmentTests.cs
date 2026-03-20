using ScvmBot.Games.MorkBorg.Reference;

namespace ScvmBot.Games.MorkBorg.Tests;

/// <summary>
/// Verifies that vignettes.json keys stay in sync with the reference data tables.
/// A failing test means something was added to one place but not the other.
/// </summary>
public class VignetteDataAlignmentTests
{
    private static readonly string DataRoot =
        TestUtilities.GetMorkBorgDataPath();

    private static Task<MorkBorgReferenceDataService> LoadAsync()
    {
        return MorkBorgReferenceDataService.CreateAsync(DataRoot);
    }

    // ── ClassIntros ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ClassIntros_NoVignetteKeyMissingFromData()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var classNames = refData.Classes.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Default", "Classless" };

        var orphaned = vignette.ClassIntros.Keys
            .Where(k => !reserved.Contains(k) && !classNames.Contains(k))
            .OrderBy(k => k)
            .ToList();

        Assert.True(orphaned.Count == 0,
            $"ClassIntros keys exist in vignettes but not in classes.json: {string.Join(", ", orphaned)}");
    }

    [Fact]
    public async Task ClassIntros_NoDataClassMissingFromVignette()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var vignetteKeys = vignette.ClassIntros.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = refData.Classes
            .Select(c => c.Name)
            .Where(name => !vignetteKeys.Contains(name))
            .OrderBy(name => name)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Classes in classes.json have no ClassIntro entry in vignettes: {string.Join(", ", missing)}");
    }

    // ── Traits ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Traits_NoVignetteKeyMissingFromData()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var dataTraits = refData.Descriptions.Trait.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphaned = vignette.Traits.Keys
            .Where(k => !dataTraits.Contains(k))
            .OrderBy(k => k)
            .ToList();

        Assert.True(orphaned.Count == 0,
            $"Trait keys exist in vignettes but not in descriptions.json Trait table: {string.Join(", ", orphaned)}");
    }

    [Fact]
    public async Task Traits_NoDataTraitMissingFromVignette()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var vignetteKeys = vignette.Traits.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = refData.Descriptions.Trait
            .Where(t => !vignetteKeys.Contains(t))
            .OrderBy(t => t)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Traits in descriptions.json have no entry in vignettes Traits: {string.Join(", ", missing)}");
    }

    // ── Bodies ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Bodies_NoVignetteKeyMissingFromData()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var dataBodies = refData.Descriptions.BrokenBody.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphaned = vignette.Bodies.Keys
            .Where(k => !dataBodies.Contains(k))
            .OrderBy(k => k)
            .ToList();

        Assert.True(orphaned.Count == 0,
            $"Body keys exist in vignettes but not in descriptions.json BrokenBody table: {string.Join(", ", orphaned)}");
    }

    [Fact]
    public async Task Bodies_NoDataBodyMissingFromVignette()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var vignetteKeys = vignette.Bodies.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = refData.Descriptions.BrokenBody
            .Where(b => !vignetteKeys.Contains(b))
            .OrderBy(b => b)
            .ToList();

        Assert.True(missing.Count == 0,
            $"BrokenBody entries in descriptions.json have no entry in vignettes Bodies: {string.Join(", ", missing)}");
    }

    // ── Habits ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Habits_NoVignetteKeyMissingFromData()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var dataHabits = refData.Descriptions.BadHabit.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphaned = vignette.Habits.Keys
            .Where(k => !dataHabits.Contains(k))
            .OrderBy(k => k)
            .ToList();

        Assert.True(orphaned.Count == 0,
            $"Habit keys exist in vignettes but not in descriptions.json BadHabit table: {string.Join(", ", orphaned)}");
    }

    [Fact]
    public async Task Habits_NoDataHabitMissingFromVignette()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var vignetteKeys = vignette.Habits.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = refData.Descriptions.BadHabit
            .Where(h => !vignetteKeys.Contains(h))
            .OrderBy(h => h)
            .ToList();

        Assert.True(missing.Count == 0,
            $"BadHabit entries in descriptions.json have no entry in vignettes Habits: {string.Join(", ", missing)}");
    }

    // ── Items (weapons) ──────────────────────────────────────────────────────

    [Fact]
    public async Task Items_NoVignetteKeyMissingFromData()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var weaponNames = refData.Weapons.Select(w => w.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        const string defaultKey = "Default";

        var orphaned = vignette.Items.Keys
            .Where(k => !k.Equals(defaultKey, StringComparison.OrdinalIgnoreCase) && !weaponNames.Contains(k))
            .OrderBy(k => k)
            .ToList();

        Assert.True(orphaned.Count == 0,
            $"Item keys exist in vignettes but not in weapons.json: {string.Join(", ", orphaned)}");
    }

    [Fact]
    public async Task Items_NoWeaponMissingFromVignette()
    {
        var refData = await LoadAsync();
        var vignette = refData.Vignettes;

        var vignetteKeys = vignette.Items.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = refData.Weapons
            .Select(w => w.Name)
            .Where(name => !vignetteKeys.Contains(name))
            .OrderBy(name => name)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Weapons in weapons.json have no entry in vignettes Items: {string.Join(", ", missing)}");
    }
}
