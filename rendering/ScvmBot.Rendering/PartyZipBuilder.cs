using System.IO.Compression;

namespace ScvmBot.Rendering;

/// <summary>
/// Creates ZIP files containing character PDFs for party downloads.
/// </summary>
public static class PartyZipBuilder
{
    /// <summary>
    /// Creates a ZIP file from pre-generated character PDFs.
    /// Duplicate character names are disambiguated with a numeric suffix.
    /// </summary>
    public static byte[] CreatePartyZip(IReadOnlyList<(ICharacter Character, byte[] PdfBytes)> members)
    {
        var entryNames = BuildUniqueEntryNames(members);

        using var memoryStream = new MemoryStream();
        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < members.Count; i++)
            {
                var entry = zipArchive.CreateEntry(entryNames[i]);
                using var entryStream = entry.Open();
                entryStream.Write(members[i].PdfBytes, 0, members[i].PdfBytes.Length);
            }
        }

        memoryStream.Position = 0;
        return memoryStream.ToArray();
    }

    internal static string[] BuildUniqueEntryNames(IReadOnlyList<(ICharacter Character, byte[] PdfBytes)> members)
    {
        var safeNames = members.Select(m => SanitizeFileName(m.Character.Name)).ToArray();

        // Count occurrences of each name to detect duplicates.
        var totalCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in safeNames)
        {
            totalCounts.TryGetValue(name, out var c);
            totalCounts[name] = c + 1;
        }

        // Assign suffixes only when a name appears more than once.
        var seenCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var entryNames = new string[members.Count];
        for (var i = 0; i < safeNames.Length; i++)
        {
            var name = safeNames[i];
            seenCounts.TryGetValue(name, out var seen);
            seen++;
            seenCounts[name] = seen;

            entryNames[i] = totalCounts[name] > 1
                ? $"{name}-{seen}.pdf"
                : $"{name}.pdf";
        }

        return entryNames;
    }

    /// <summary>
    /// Generates a file name for a party ZIP attachment.
    /// </summary>
    public static string GeneratePartyZipFileName(string partyName)
    {
        var safeName = SanitizeFileName(partyName);
        return $"{safeName}.zip";
    }

    internal static string SanitizeFileName(string name, string fallback = "party")
    {
        if (string.IsNullOrWhiteSpace(name))
            return fallback;

        var sanitized = new string(name
            .Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_')
            .ToArray())
            .Trim('_');

        return string.IsNullOrEmpty(sanitized) ? fallback : sanitized;
    }
}
