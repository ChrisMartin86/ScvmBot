namespace ScvmBot.Rendering;

/// <summary>
/// A renderer that consumes a <see cref="GenerateResult"/> and produces
/// a specific <see cref="OutputFormat"/>.
/// Implementations are strongly typed to the data they support via
/// <see cref="CanRender"/>; the <see cref="RendererRegistry"/> uses this
/// to select the correct renderer at runtime.
/// </summary>
public interface IResultRenderer
{
    /// <summary>The output format this renderer produces.</summary>
    OutputFormat Format { get; }

    /// <summary>Returns true when this renderer can handle the given result.</summary>
    bool CanRender(GenerateResult result);

    /// <summary>
    /// Renders the result into an output.
    /// Throws <see cref="InvalidOperationException"/> if the result type is unsupported.
    /// </summary>
    RenderOutput Render(GenerateResult result);
}
