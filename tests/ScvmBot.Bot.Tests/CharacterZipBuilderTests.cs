using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Services;
using ScvmBot.Modules;
using System.IO.Compression;

namespace ScvmBot.Bot.Tests;

// =====================================================================
// CharacterZipBuilderTests
// =====================================================================
public class CharacterZipBuilderTests
{
    [Fact]
    public void CreateZip_IncludesAllCharacterPdfs()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var members = new List<(string, byte[])>
        {
            ("Alpha", pdfBytes),
            ("Beta", pdfBytes)
        };

        var zipBytes = CharacterZipBuilder.CreateZip(members);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Equal(2, archive.Entries.Count);
    }

    [Fact]
    public void CreateZip_NamesEntriesCorrectly_Format()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var members = new List<(string, byte[])>
        {
            ("Svein", pdfBytes)
        };

        var zipBytes = CharacterZipBuilder.CreateZip(members);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Single(archive.Entries);
        Assert.Equal("Svein.pdf", archive.Entries[0].FullName);
    }

    [Fact]
    public void CreateZip_OnlyIncludesProvidedMembers()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        // Only include one character (simulating caller filtering out null PDFs)
        var members = new List<(string, byte[])>
        {
            ("Has PDF", pdfBytes)
        };

        var zipBytes = CharacterZipBuilder.CreateZip(members);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Single(archive.Entries);
        Assert.Contains("Has_PDF", archive.Entries[0].FullName);
    }

    [Fact]
    public void GenerateZipFileName_FormatsSafely_RemovesSpecialChars()
    {
        var result = CharacterZipBuilder.GenerateZipFileName("Kärg's Crew!");

        Assert.DoesNotContain("'", result);
        Assert.DoesNotContain("!", result);
        Assert.EndsWith(".zip", result);
    }

    [Fact]
    public void GenerateZipFileName_FollowsNamingConvention()
    {
        var result = CharacterZipBuilder.GenerateZipFileName("TestGroup");

        Assert.Equal("TestGroup.zip", result);
    }

    [Theory]
    [InlineData("@#$", "characters")]
    [InlineData("!!!", "characters")]
    [InlineData("---", "---")]
    [InlineData("_name_", "name")]
    public void SanitizeFileName_ReturnsNonEmpty_WhenInputReducesToAllUnderscores(string name, string expected)
    {
        var result = CharacterZipBuilder.SanitizeFileName(name);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeFileName_UsesCustomFallback_WhenResultIsEmpty()
    {
        var result = CharacterZipBuilder.SanitizeFileName("@#$", fallback: "character");

        Assert.Equal("character", result);
    }
}

// =====================================================================
// Shared Fakes
// =====================================================================

internal class FakeCharacter
{
    public string Name { get; set; } = "";
}

internal class FakeGameSystem : IGameModule
{
    public string CommandKey => "test";
    public string Name => "Test System";

    public IReadOnlyList<SubCommandDefinition> SubCommands =>
        throw new NotImplementedException();

    public Task<GenerateResult> HandleGenerateCommandAsync(
        string subCommand,
        IReadOnlyDictionary<string, object?> options,
        CancellationToken ct = default) =>
        throw new NotImplementedException();
}

internal class FakeLogger : ILogger<GenerateCommandHandler>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    { }
}
