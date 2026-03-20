namespace ScvmBot.Rendering;

/// <summary>
/// Selects an appropriate <see cref="IResultRenderer"/> based on the generated
/// result type and desired output format.
/// </summary>
public sealed class RendererRegistry
{
    private readonly IReadOnlyList<IResultRenderer> _renderers;

    public RendererRegistry(IEnumerable<IResultRenderer> renderers) =>
        _renderers = renderers.ToList();

    /// <summary>
    /// Returns the first renderer that supports the given result and format,
    /// or null if no renderer matches.
    /// </summary>
    public IResultRenderer? FindRenderer(GenerateResult result, OutputFormat format) =>
        _renderers.FirstOrDefault(r => r.Format == format && r.CanRender(result));

    /// <summary>
    /// Returns the first renderer that supports the given result and format.
    /// Throws <see cref="InvalidOperationException"/> if no renderer matches.
    /// </summary>
    public IResultRenderer GetRequiredRenderer(GenerateResult result, OutputFormat format) =>
        FindRenderer(result, format)
            ?? throw new InvalidOperationException(
                $"No renderer registered for {result.GetType().Name} with format {format}.");
}
