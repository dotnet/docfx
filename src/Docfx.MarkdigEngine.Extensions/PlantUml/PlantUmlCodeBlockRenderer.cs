// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using PlantUml.Net;
using static System.Text.Encoding;

namespace Docfx.MarkdigEngine.Extensions;

/// <summary>
/// An HTML renderer for a <see cref="CodeBlock"/> and <see cref="FencedCodeBlock"/>.
/// </summary>
/// <seealso cref="HtmlObjectRenderer{CodeBlock}" />
class PlantUmlCodeBlockRenderer : CodeBlockRenderer
{
    private readonly MarkdownContext _context;
    private readonly PlantUmlSettings _settings;
    private readonly OutputFormat _outputFormat;
    private readonly RendererFactory rendererFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBlockRenderer"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="settings"></param>
    public PlantUmlCodeBlockRenderer(MarkdownContext context, PlantUmlOptions settings)
    {
        _context = context;
        _settings = new()
        {
            Delimitor = settings.Delimitor,
            RenderingMode = settings.RenderingMode,
            RemoteUrl = settings.RemoteUrl,
            JavaPath = settings.JavaPath,
            LocalGraphvizDotPath = settings.LocalGraphvizDotPath,
            LocalPlantUmlPath = settings.LocalPlantUmlPath,
        };
        _outputFormat = settings.OutputFormat;

        rendererFactory = new RendererFactory();
    }

    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        if (obj is FencedCodeBlock { Info: string info } fencedCodeBlock
            && info.Equals("plantuml", StringComparison.OrdinalIgnoreCase))
        {
            IPlantUmlRenderer plantUmlRenderer = rendererFactory.CreateRenderer(_settings);

            // Get PlantUML code.
            var plantUmlCode = fencedCodeBlock.Lines.ToString();

            try
            {
                byte[] output = plantUmlRenderer.Render(plantUmlCode, _outputFormat);

                renderer.EnsureLine();
                renderer.Write(FormatOutput(_outputFormat, output));
                renderer.EnsureLine();
            }
            catch (RenderingException ex)
            {
                _context.LogWarning(nameof(PlantUmlExtension), ex.Message, null);
            }
            catch (Exception ex)
            {
                _context.LogError(nameof(PlantUmlExtension), ex.Message, null);

                // If the error is not related to rendering a specific diagram, re-throw to abort
                throw;
            }

            return;
        }

        // Fallback to default CodeBlockRenderer
        base.Write(renderer, obj);
    }

    private static string FormatOutput(OutputFormat format, byte[] output)
    {
        switch (format)
        {
            case OutputFormat.Svg:
                string svg = UTF8.GetString(output);
                return $"<div class=\"lang-plantUml\">{svg}</div>";

            case OutputFormat.Ascii:
                string ascii = ASCII.GetString(output);
                return $"<div class=\"lang-plantUml\"><pre>{ascii}</pre></div>";

            case OutputFormat.Ascii_Unicode:
                string asciiUnicode = UTF8.GetString(output);
                return $"<div class=\"lang-plantUml\"><pre>{asciiUnicode}</pre></div>";

            case OutputFormat.Png:
            case OutputFormat.Eps:
            case OutputFormat.Pdf:
            case OutputFormat.Vdx:
            case OutputFormat.Xmi:
            case OutputFormat.Scxml:
            case OutputFormat.Html:
            case OutputFormat.LaTeX:
            default:
                throw new NotSupportedException($"Output format {format} is currently not supported");
        }
    }
}
