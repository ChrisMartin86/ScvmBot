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
    /// Validates that no two renderers claim the same concrete result type and format combination.
    /// This is checked eagerly at startup to surface configuration mistakes early.
    /// </summary>
    private void ValidateNoAmbiguousRenderers()
    {
        var seen = new Dictionary<(Type, OutputFormat), IResultRenderer>();
        foreach (var renderer in _renderers)
        {
            var key = (renderer.GetType(), renderer.Format);
            if (!seen.TryAdd(key, renderer))
            {
                throw new InvalidOperationException(
                    $"Ambiguous renderer registration: '{renderer.GetType().Name}' is registered " +
                    $"more than once for format {renderer.Format}. Each renderer type may only be registered once.");
            }
        }
    }

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

    /// <summary>
    /// Renders the result as a Discord embed. Throws if no embed renderer matches.
    /// </summary>
    public EmbedOutput RenderEmbed(GenerateResult result)
    {
        var renderer = GetRequiredRenderer(result, OutputFormat.DiscordEmbed);
        var output = renderer.Render(result);
        return output as EmbedOutput
            ?? throw new InvalidOperationException(
                $"Renderer {renderer.GetType().Name} declared OutputFormat.DiscordEmbed but returned {output.GetType().Name}.");
    }

    /// <summary>
    /// Attempts to render the result as a file (PDF, ZIP, etc.).
    /// Returns null if no file renderer matches.
    /// </summary>
    public FileOutput? TryRenderFile(GenerateResult result)
    {
        var renderer = FindRenderer(result, OutputFormat.Pdf);
        if (renderer is null)
            return null;

        var output = renderer.Render(result);
        return output as FileOutput
            ?? throw new InvalidOperationException(
                $"Renderer {renderer.GetType().Name} declared OutputFormat.Pdf but returned {output.GetType().Name}.");
    }
}
