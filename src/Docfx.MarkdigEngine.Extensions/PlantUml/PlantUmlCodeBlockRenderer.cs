using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Renderers.Html;
using PlantUml.Net;

namespace Docfx.MarkdigEngine.Extensions;

/// <summary>
/// An HTML renderer for a <see cref="CodeBlock"/> and <see cref="FencedCodeBlock"/>.
/// </summary>
/// <seealso cref="HtmlObjectRenderer{CodeBlock}" />
public class CustomCodeBlockRenderer : CodeBlockRenderer
{
    private readonly MarkdownContext _context;
    private readonly DocfxPlantUmlSettings _settings;
    private readonly RendererFactory rendererFactory;
    private readonly FormatterFactory formatterFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBlockRenderer"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="settings"></param>
    public CustomCodeBlockRenderer(MarkdownContext context, DocfxPlantUmlSettings settings)
    {
        _context = context;
        _settings = settings;

        rendererFactory = new RendererFactory();
        formatterFactory = new FormatterFactory(settings);
    }

    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        if (obj is FencedCodeBlock fencedCodeBlock
          && fencedCodeBlock.Info is string info
          && info.Equals("plantuml", StringComparison.OrdinalIgnoreCase))
        {
            IPlantUmlRenderer plantUmlRenderer = rendererFactory.CreateRenderer(_settings);
            IOutputFormatter outputFormatter = formatterFactory.CreateOutputFormatter();

            // Get PlantUML code.
            var plantUmlCode = fencedCodeBlock.Lines.ToString();

            byte[] output = plantUmlRenderer.Render(plantUmlCode, _settings.OutputFormat);

            renderer.EnsureLine();
            renderer.Write(outputFormatter.FormatOutput(output));
            renderer.EnsureLine();

            return;
        }

        // Fallback to default CodeBlockRenderer
        base.Write(renderer, obj);
    }
}
