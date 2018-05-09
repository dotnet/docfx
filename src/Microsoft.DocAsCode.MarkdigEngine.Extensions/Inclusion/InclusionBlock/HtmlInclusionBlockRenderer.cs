// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Linq;
    using System.Text.RegularExpressions;

    using Markdig;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;

    public class HtmlInclusionBlockRenderer : HtmlObjectRenderer<InclusionBlock>
    {
        private MarkdownContext _context;
        private MarkdownPipeline _pipeline;
        private Regex YamlHeaderRegex = new Regex(@"^<yamlheader[^>]*?>[\s\S]*?<\/yamlheader>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public HtmlInclusionBlockRenderer(MarkdownContext context, MarkdownPipeline pipeline)
        {
            _context = context;
            _pipeline = pipeline;
        }

        protected override void Write(HtmlRenderer renderer, InclusionBlock inclusion)
        {
            var (content, includeFilePath) = _context.ReadFile(inclusion.IncludedFilePath, _context.File);

            if (content == null)
            {
                Logger.LogWarning($"Cannot resolve '{inclusion.IncludedFilePath}' relative to '{_context.File}'.");
                renderer.Write(inclusion.GetRawToken());
                return;
            }

            if (_context.CircularReferenceDetector.Contains(includeFilePath))
            {
                Logger.LogWarning($"Found circular reference: {string.Join(" -> ", _context.CircularReferenceDetector)} -> {includeFilePath}\"");
                renderer.Write(inclusion.GetRawToken());
                return;
            }

            _context.Dependencies.Add(includeFilePath);

            var context = new MarkdownContext(
                includeFilePath,
                _context.IsInline,
                _context.EnableSourceInfo,
                _context.Tokens,
                _context.Mvb,
                _context.EnableValidation,
                _context.ReadFile,
                _context.GetLink,
                _context.GetFilePath,
                _context.CircularReferenceDetector,
                _context.Dependencies);

            var pipeline = new MarkdownPipelineBuilder()
                .UseDocfxExtensions(context)
                .Build();

            var result = Markdown.ToHtml(content, pipeline);
            result = SkipYamlHeader(result);

            renderer.Write(result);
        }

        private string SkipYamlHeader(string content)
        {
            return YamlHeaderRegex.Replace(content, "");
        }
    }
}