using System.Text.Json.Serialization;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using PlantUml.Net;

namespace Docfx.MarkdigEngine.Extensions;

public class PlantUmlOptions
{
    [JsonPropertyName("javaPath")]
    public string JavaPath { get; set; }

    [JsonPropertyName("remoteUrl")]
    public string RemoteUrl { get; set; }

    [JsonPropertyName("localPlantUmlPath")]
    public string LocalPlantUmlPath { get; set; }

    [JsonPropertyName("localGraphvizDotPath")]
    public string LocalGraphvizDotPath { get; set; }

    [JsonPropertyName("renderingMode")]
    public RenderingMode RenderingMode { get; set; }

    [JsonPropertyName("delimitor")]
    public string Delimitor { get; set; }

    [JsonPropertyName("outputFormat")]
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Svg;
}

internal class PlantUmlExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;
    private readonly PlantUmlOptions _settings;

    public PlantUmlExtension(MarkdownContext context, PlantUmlOptions settings)
    {
        _context = context;
        _settings = settings ?? new();
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer { ObjectRenderers: not null } htmlRenderer)
        {
            var customRenderer = new PlantUmlCodeBlockRenderer(_context, _settings);
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
