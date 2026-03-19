using Discord;
using Microsoft.Extensions.Logging;
using ScvmBot.Bot.Games;
using ScvmBot.Bot.Models;
using ScvmBot.Bot.Services;
using System.IO.Compression;

namespace ScvmBot.Bot.Tests;

// =====================================================================
// PartyZipBuilderTests
// =====================================================================
public class PartyZipBuilderTests
{
    [Fact]
    public void CreatePartyZip_IncludesAllCharacterPdfs()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var members = new List<(ICharacter, byte[])>
        {
            (new FakeCharacter { Name = "Alpha" }, pdfBytes),
            (new FakeCharacter { Name = "Beta" }, pdfBytes)
        };

        var zipBytes = PartyZipBuilder.CreatePartyZip(members);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Equal(2, archive.Entries.Count);
    }

    [Fact]
    public void CreatePartyZip_NamesEntriesCorrectly_Format()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var members = new List<(ICharacter, byte[])>
        {
            (new FakeCharacter { Name = "Svein" }, pdfBytes)
        };

        var zipBytes = PartyZipBuilder.CreatePartyZip(members);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Single(archive.Entries);
        Assert.Equal("Svein.pdf", archive.Entries[0].FullName);
    }

    [Fact]
    public void CreatePartyZip_OnlyIncludesProvidedMembers()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        // Only include one character (simulating caller filtering out null PDFs)
        var members = new List<(ICharacter, byte[])>
        {
            (new FakeCharacter { Name = "Has PDF" }, pdfBytes)
        };

        var zipBytes = PartyZipBuilder.CreatePartyZip(members);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Single(archive.Entries);
        Assert.Contains("Has_PDF", archive.Entries[0].FullName);
    }

    [Fact]
    public void GeneratePartyZipFileName_FormatsSafely_RemovesSpecialChars()
    {
        var result = PartyZipBuilder.GeneratePartyZipFileName("Kärg's Crew!");

        Assert.DoesNotContain("'", result);
        Assert.DoesNotContain("!", result);
        Assert.EndsWith(".zip", result);
    }

    [Fact]
    public void GeneratePartyZipFileName_FollowsNamingConvention()
    {
        var result = PartyZipBuilder.GeneratePartyZipFileName("TestParty");

        Assert.Equal("TestParty.zip", result);
    }
}

// =====================================================================
// Shared Fakes
// =====================================================================

internal class FakeCharacter : ICharacter
{
    public string Name { get; set; } = "";
}

internal class FakeGameSystem : IGameSystem
{
    public string CommandKey => "test";
    public string Name => "Test System";
    public bool SupportsPdf => true;

    public SlashCommandOptionBuilder BuildCommandGroupOptions() =>
        throw new NotImplementedException();

    public Task<GenerateResult> HandleGenerateCommandAsync(
        IReadOnlyCollection<IApplicationCommandInteractionDataOption>? options,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public string BuildFileName(ICharacter character) =>
        $"{character.Name}.pdf";

    public byte[]? GeneratePdf(ICharacter character)
    {
        return new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
    }
}

internal class FakeLogger : ILogger<GenerateCommandHandler>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    { }
}
