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
