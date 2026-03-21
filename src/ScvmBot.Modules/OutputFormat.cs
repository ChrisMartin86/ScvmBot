namespace ScvmBot.Modules;

/// <summary>Discriminates the output format a renderer produces.</summary>
public enum OutputFormat
{
    /// <summary>A structured card with title, description, fields, etc.</summary>
    Card,

    /// <summary>A downloadable file attachment (PDF, ZIP, etc.).</summary>
    File
}
