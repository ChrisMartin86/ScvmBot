using Discord;

namespace ScvmBot.Rendering;

/// <summary>Base type for renderer outputs.</summary>
public abstract record RenderOutput;

/// <summary>A rendered Discord embed.</summary>
public sealed record EmbedOutput(Embed Embed) : RenderOutput;

/// <summary>A rendered file (PDF, ZIP, etc.).</summary>
public sealed record FileOutput(byte[] Bytes, string FileName) : RenderOutput;
