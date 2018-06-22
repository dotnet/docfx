namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Extensions.CustomContainers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System.Collections.Generic;
    using System.Linq;
    using static Microsoft.DocAsCode.MarkdigEngine.Extensions.MarkdownContext;

    public class TripleColonExtension : IMarkdownExtension
    {
        private readonly MarkdownContext _context;
        private readonly IDictionary<string, ITripleColonExtensionInfo> _extensions;

        public TripleColonExtension(MarkdownContext context)
        {
            _context = context;
            _extensions = (new ITripleColonExtensionInfo[]
            {
                new ZoneExtension()
                // todo: moniker range, row, etc...
            }).ToDictionary(x => x.Name);
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var parser = new TripleColonParser(_context, _extensions);
            if (pipeline.BlockParsers.Contains<CustomContainerParser>())
            {
                pipeline.BlockParsers.InsertBefore<CustomContainerParser>(parser);
            }
            else
            {
                pipeline.BlockParsers.AddIfNotAlready(parser);
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var htmlRenderer = renderer as HtmlRenderer;
            if (htmlRenderer != null && !htmlRenderer.ObjectRenderers.Contains<TripleColonRenderer>())
            {
                htmlRenderer.ObjectRenderers.Insert(0, new TripleColonRenderer());
            }
        }
    }

    public interface ITripleColonExtensionInfo
    {
        string Name { get; }
        bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, LogActionDelegate logError);
        bool TryValidateAncestry(ContainerBlock container, LogActionDelegate logError);
        // todo: "Render" function as-needed.
    }
}
