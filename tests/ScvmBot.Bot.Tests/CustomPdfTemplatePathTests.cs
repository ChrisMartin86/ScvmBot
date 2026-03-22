using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScvmBot.Games.MorkBorg.Models;
using ScvmBot.Games.MorkBorg.Pdf;
using ScvmBot.Modules;
using ScvmBot.Modules.MorkBorg;

namespace ScvmBot.Bot.Tests;

/// <summary>
/// Proves the custom PDF template path behavior end-to-end:
/// when a custom DataPath directory contains character_sheet.pdf,
/// MorkBorgModuleRegistration resolves it and the resulting renderer
/// reports TemplateExists == true.
/// </summary>
public class CustomPdfTemplatePathTests
{
    private static IConfiguration BuildConfig(string dataPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Modules:MorkBorg:DataPath"] = dataPath })
            .Build();

    [Fact]
    public async Task Registration_UsesCustomPdfTemplate_WhenPresentInDataPath()
    {
        var dir = TestInfrastructure.CreateTempDirectory();

        // Create minimal data files
        await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

        // Place a dummy PDF template in the custom data directory
        var templatePath = Path.Combine(dir, "character_sheet.pdf");
        await File.WriteAllBytesAsync(templatePath, CreateMinimalPdf());

        var register = await new MorkBorgModuleRegistration().InitializeAsync(BuildConfig(dir));

        var services = new ServiceCollection();
        register(services);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        using var provider = services.BuildServiceProvider();
        var pdfRenderer = provider.GetRequiredService<MorkBorgPdfRenderer>();

        Assert.True(pdfRenderer.TemplateExists,
            "MorkBorgPdfRenderer.TemplateExists should be true when custom DataPath contains character_sheet.pdf");
    }

    [Fact]
    public async Task RenderFile_UsesCustomTemplate_EndToEnd()
    {
        var dir = TestInfrastructure.CreateTempDirectory();

        await File.WriteAllTextAsync(Path.Combine(dir, "classes.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "spells.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "names.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "weapons.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "armor.json"), "[]");
        await File.WriteAllTextAsync(Path.Combine(dir, "items.json"), "[]");

        // Copy the real PDF template from the repo (if available)
        var repoDataPath = Path.Combine(
            SharedTestInfrastructure.GetRepositoryRoot(),
            "src", "ScvmBot.Games.MorkBorg", "Data");
        var realTemplate = Path.Combine(repoDataPath, "character_sheet.pdf");
        if (!File.Exists(realTemplate))
            return; // Skip if PDF template not available in this environment

        File.Copy(realTemplate, Path.Combine(dir, "character_sheet.pdf"));

        var register = await new MorkBorgModuleRegistration().InitializeAsync(BuildConfig(dir));

        var services = new ServiceCollection();
        register(services);
        services.AddSingleton<RendererRegistry>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        using var provider = services.BuildServiceProvider();
        var module = provider.GetRequiredService<IGameModule>();
        var registry = provider.GetRequiredService<RendererRegistry>();

        var result = await module.HandleGenerateCommandAsync("character", new Dictionary<string, object?>());
        var file = registry.TryRenderFile(result);

        Assert.NotNull(file);
        Assert.True(file!.Bytes.Length > 0);
        Assert.EndsWith(".pdf", file.FileName);
    }

    // Minimal valid-ish PDF bytes (enough for File.Exists to return true)
    private static byte[] CreateMinimalPdf() =>
        System.Text.Encoding.ASCII.GetBytes(
            "%PDF-1.0\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
            "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
            "3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj\n" +
            "xref\n0 4\n0000000000 65535 f \n0000000009 00000 n \n" +
            "0000000058 00000 n \n0000000115 00000 n \n" +
            "trailer<</Size 4/Root 1 0 R>>\nstartxref\n190\n%%EOF");
}
