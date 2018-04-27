// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class HtmlInclusionInlineRenderer : HtmlObjectRenderer<InclusionInline>
    {
        private readonly MarkdownContext _context;
        private readonly MarkdownPipeline _pipeline;

        public HtmlInclusionInlineRenderer(MarkdownContext context, MarkdownPipeline pipeline)
        {
            _context = context;
            _pipeline = pipeline;
        }

        protected override void Write(HtmlRenderer renderer, InclusionInline inclusion)
        {
            if (string.IsNullOrEmpty(inclusion.Context.IncludedFilePath))
            {
                Logger.LogError("file path can't be empty or null in IncludeFile");
                renderer.Write(inclusion.Context.GetRaw());

                return;
            }

            if (!PathUtility.IsRelativePath(inclusion.Context.IncludedFilePath))
            {
                string tag = "ERROR INCLUDE";
                string message = $"Unable to resolve {inclusion.Context.GetRaw()}: Absolute path \"{inclusion.Context.IncludedFilePath}\" is not supported.";
                ExtensionsHelper.GenerateNodeWithCommentWrapper(renderer, tag, message, inclusion.Context.GetRaw(), inclusion.Line);

                return;
            }

            var currentFilePath = ((RelativePath)_context.FilePath).GetPathFromWorkingFolder();
            var includedFilePath = ((RelativePath)inclusion.Context.IncludedFilePath).BasedOn(currentFilePath);

            if (!EnvironmentContext.FileAbstractLayer.Exists(includedFilePath.RemoveWorkingFolder()))
            {
                Logger.LogWarning($"Can't find {includedFilePath}.");
                renderer.Write(inclusion.Context.GetRaw());

                return;
            }

            if (!_context.Dependencies.Add(includedFilePath))
            {
                string tag = "ERROR INCLUDE";
                string message = $"Unable to resolve {inclusion.Context.GetRaw()}: Circular dependency found in \"{_context.FilePath}\"";
                ExtensionsHelper.GenerateNodeWithCommentWrapper(renderer, tag, message, inclusion.Context.GetRaw(), inclusion.Line);

                return;
            }

            var content = EnvironmentContext.FileAbstractLayer.ReadAllText(includedFilePath.RemoveWorkingFolder());
            var context = new MarkdownContext(
                content,
                _context.BasePath,
                includedFilePath.RemoveWorkingFolder(),
                true,
                _context.Dependencies,
                _context.EnableSourceInfo,
                _context.Tokens,
                _context.Mvb);

            var pipeline = new MarkdownPipelineBuilder()
                .UseDocfxExtensions(context)
                .Build();



            // Do not need to check if content is a single paragragh
            // context.IsInline = true will force it into a single paragragh and render with no <p></p>
            var result = Markdown.ToHtml(content, pipeline);
            renderer.Write(result);
        }
    }
}
