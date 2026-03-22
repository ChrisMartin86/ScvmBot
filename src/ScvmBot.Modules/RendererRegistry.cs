namespace ScvmBot.Modules;

/// <summary>
/// Selects an appropriate <see cref="IResultRenderer"/> based on the generated
/// result type and desired output format.
/// </summary>
public sealed class RendererRegistry
{
    private readonly IReadOnlyList<IResultRenderer> _renderers;

    public RendererRegistry(IEnumerable<IResultRenderer> renderers)
    {
        _renderers = renderers.ToList();
        ValidateNoAmbiguousRenderers();
    }

    /// <summary>
    /// Validates that no two renderers claim the same (ResultType, Format) combination.
    /// This is checked eagerly at startup to surface configuration mistakes early.
    /// </summary>
    private void ValidateNoAmbiguousRenderers()
    {
        var seen = new Dictionary<(Type, OutputFormat), IResultRenderer>();
        foreach (var renderer in _renderers)
        {
            var key = (renderer.ResultType, renderer.Format);
            if (!seen.TryAdd(key, renderer))
            {
                var existing = seen[key];
                throw new InvalidOperationException(
                    $"Ambiguous renderer registration: both '{existing.GetType().Name}' and '{renderer.GetType().Name}' " +
                    $"claim result type {renderer.ResultType.Name} with format {renderer.Format}. " +
                    $"Each (ResultType, Format) pair must have exactly one renderer.");
            }
        }
    }

    /// <summary>
    /// Returns the first renderer that supports the given result and format,
    /// or null if no renderer matches.
    /// Uses ResultType as the primary match (consistent with startup validation),
    /// with CanRender as a secondary confirmation.
    /// </summary>
    public IResultRenderer? FindRenderer(GenerateResult result, OutputFormat format) =>
        _renderers.FirstOrDefault(r => r.Format == format
            && r.ResultType == result.GetType()
            && r.CanRender(result));

    /// <summary>
    /// Returns the first renderer that supports the given result and format.
    /// Throws <see cref="InvalidOperationException"/> if no renderer matches.
    /// </summary>
    public IResultRenderer GetRequiredRenderer(GenerateResult result, OutputFormat format) =>
        FindRenderer(result, format)
            ?? throw new InvalidOperationException(
                $"No renderer registered for {result.GetType().Name} with format {format}.");

    /// <summary>
    /// Renders the result as a structured card. Throws if no card renderer matches.
    /// </summary>
    public CardOutput RenderCard(GenerateResult result)
    {
        var renderer = GetRequiredRenderer(result, OutputFormat.Card);
        var output = renderer.Render(result);
        return output as CardOutput
            ?? throw new InvalidOperationException(
                $"Renderer {renderer.GetType().Name} declared OutputFormat.Card but returned {output.GetType().Name}.");
    }

    /// <summary>
    /// Attempts to render the result as a downloadable file (PDF, ZIP, etc.).
    /// Returns null if no file renderer matches.
    /// </summary>
    public FileOutput? TryRenderFile(GenerateResult result)
    {
        var renderer = FindRenderer(result, OutputFormat.File);
        if (renderer is null)
            return null;

        var output = renderer.Render(result);
        return output as FileOutput
            ?? throw new InvalidOperationException(
                $"Renderer {renderer.GetType().Name} declared OutputFormat.File but returned {output.GetType().Name}.");
    }
}
