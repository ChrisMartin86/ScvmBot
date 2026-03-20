using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using System.Diagnostics.CodeAnalysis;

using ScvmBot.Games.MorkBorg.Models;

namespace ScvmBot.Games.MorkBorg.Pdf;

/// <summary>Extension methods for filling the MÖRK BORG AcroForm character sheet PDF.</summary>
[ExcludeFromCodeCoverage(Justification = "Requires a real AcroForm PDF file to test; covered by integration testing.")]
public static class PdfCharacterSheetExtensions
{
    // All field names that must be present in the PDF.
    private static readonly IReadOnlyList<string> RequiredFields = BuildRequiredFieldList();

    /// <summary>
    /// Fills every AcroForm field in the MÖRK BORG character sheet PDF from <paramref name="data"/>.
    /// </summary>
    /// <param name="pdfBytes">Source PDF bytes. Must contain the expected AcroForm fields.</param>
    /// <param name="data">Strongly-typed field values.</param>
    /// <param name="flatten">When true the form is flattened (fields become static content).</param>
    /// <returns>Byte array of the filled (and optionally flattened) PDF.</returns>
    public static byte[] FillMorkBorgSheet(this byte[] pdfBytes, CharacterSheetData data, bool flatten = true)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var outputStream = new MemoryStream();

        using var reader = new PdfReader(inputStream);
        using var writer = new PdfWriter(outputStream);
        using var pdfDoc = new PdfDocument(reader, writer);

        var form = PdfAcroForm.GetAcroForm(pdfDoc, false)
            ?? throw new InvalidOperationException("The PDF does not contain an AcroForm.");

        var fields = form.GetAllFormFields();

        // Validate that every expected field is present before writing anything.
        var missing = RequiredFields.Where(name => !fields.ContainsKey(name)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"The PDF is missing {missing.Count} expected field(s):\n  {string.Join("\n  ", missing)}");


        SetText(fields, "name", data.Name);
        SetText(fields, "hp_current", data.HP_Current);
        SetText(fields, "hp_max", data.HP_Max);
        SetText(fields, "strength", data.Strength);
        SetText(fields, "agility", data.Agility);
        SetText(fields, "presence", data.Presence);
        SetText(fields, "toughness", data.Toughness);
        SetText(fields, "omens", data.Omens);
        SetText(fields, "silver", data.Silver);
        SetText(fields, "class", data.ClassName);

        SetText(fields, "description", data.Description);
        SetText(fields, "armor_name", data.ArmorText);

        for (var i = 0; i < 2; i++)
            SetText(fields, $"weapon_{i + 1}", data.Weapons.Length > i ? data.Weapons[i] : string.Empty);

        for (var i = 0; i < 4; i++)
            SetText(fields, $"powers_{i + 1}", data.Powers.Length > i ? data.Powers[i] : string.Empty);

        for (var i = 0; i < 15; i++)
            SetText(fields, $"equipment_{i + 1}", data.Equipment.Length > i ? data.Equipment[i] ?? string.Empty : string.Empty);

        // Armor tier: check only the matching die checkbox (d2=light, d4=medium, d6=heavy)
        var armorCheckboxNames = new[] { "armor_d2", "armor_d4", "armor_d6" };
        for (var i = 0; i < 3; i++)
            SetCheckbox(fields, armorCheckboxNames[i], i == data.ArmorTier - 1);

        for (var i = 0; i < 6; i++)
            SetCheckbox(fields, $"misery_{i + 1}", data.Miseries.Contains(i));

        if (!flatten)
            form.SetNeedAppearances(true);
        else
            form.FlattenFields();

        pdfDoc.Close();
        return outputStream.ToArray();
    }


    public static byte[] FillMorkBorgSheet(this byte[] pdfBytes, Character character, bool flatten = true) =>
        pdfBytes.FillMorkBorgSheet(CharacterSheetMapper.Map(character), flatten);

    private static void SetText(IDictionary<string, PdfFormField> fields, string name, string value) =>
        fields[name].SetValue(value ?? string.Empty);

    private static void SetCheckbox(IDictionary<string, PdfFormField> fields, string name, bool check) =>
        fields[name].SetValue(check ? "Yes" : "Off");

    private static IReadOnlyList<string> BuildRequiredFieldList()
    {
        var list = new List<string>
        {
            "name", "hp_current", "hp_max",
            "strength", "agility", "presence", "toughness",
            "omens", "silver", "class",
            "description", "armor_name",
            "weapon_1", "weapon_2",
            "powers_1", "powers_2", "powers_3", "powers_4",
        };

        for (var i = 1; i <= 15; i++)
            list.Add($"equipment_{i}");

        list.AddRange(new[] { "armor_d2", "armor_d4", "armor_d6" });
        for (var i = 1; i <= 6; i++) list.Add($"misery_{i}");

        return list.AsReadOnly();
    }
}
