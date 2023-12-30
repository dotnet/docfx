using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using PlantUml.Net;

namespace Docfx.MarkdigEngine.Extensions;

public class DocfxPlantUmlSettings : PlantUmlSettings
{
    public DocfxPlantUmlSettings() : base()
    {
    }

    public DocfxPlantUmlSettings(IReadOnlyDictionary<string, string> config) : this()
    {
        if (config.TryGetValue("remoteUrl", out var url))
            RemoteUrl = url;
        if (config.TryGetValue("outputFormat", out var format))
            OutputFormat = Enum.Parse<OutputFormat>(format, true);
        if (config.TryGetValue("javaPath", out var path))
            JavaPath = path;
        if (config.TryGetValue("localPlantUmlPath", out path))
            LocalPlantUmlPath = path;
        if (config.TryGetValue("localGraphvizDotPath", out path))
            LocalGraphvizDotPath = path;
        if (config.TryGetValue("renderingMode", out var renderMode))
            RenderingMode = Enum.Parse<RenderingMode>(renderMode, true);
    }

    public OutputFormat OutputFormat { get; set; } = OutputFormat.Svg;
}

internal class PlantUmlExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;
    private readonly DocfxPlantUmlSettings _settings;

    public PlantUmlExtension(MarkdownContext context)
    {
        _context = context;
        _settings = new();

        var config = _context.GetExtensionConfiguration("PlantUml");
        if (config != null)
            _settings = new DocfxPlantUmlSettings(config);
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer { ObjectRenderers: not null } htmlRenderer)
        {
            var customRenderer = new CustomCodeBlockRenderer(_context, _settings);
            var renderers = htmlRenderer.ObjectRenderers;

            if (renderers.Contains<CodeBlockRenderer>())
            {
                renderers.InsertBefore<CodeBlockRenderer>(customRenderer);
            }
            else
            {
                renderers.AddIfNotAlready(customRenderer);
            }
        }
    }
}
