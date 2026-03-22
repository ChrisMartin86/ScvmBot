namespace ScvmBot.Modules;

/// <summary>Base type for renderer outputs.</summary>
public abstract record RenderOutput;

/// <summary>A rendered structured card (title, description, fields, etc.).</summary>
public sealed record CardOutput(
    string? Title = null,
    string? Description = null,
    string? Footer = null,
    CardColor? Color = null,
    IReadOnlyList<CardField>? Fields = null) : RenderOutput;

/// <summary>A single field in a <see cref="CardOutput"/>.</summary>
public sealed record CardField(string Name, string Value, bool Inline = false);

/// <summary>RGB color for a <see cref="CardOutput"/>.</summary>
public sealed record CardColor(byte R, byte G, byte B);

/// <summary>A rendered file (PDF, ZIP, etc.).</summary>
public sealed record FileOutput(byte[] Bytes, string FileName) : RenderOutput;
