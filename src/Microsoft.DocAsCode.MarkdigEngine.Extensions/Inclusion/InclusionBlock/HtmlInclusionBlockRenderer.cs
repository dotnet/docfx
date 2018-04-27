// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Text.RegularExpressions;

    using Markdig;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

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
            if (string.IsNullOrEmpty(inclusion.Context.IncludedFilePath))
            {
                Logger.LogError("file path can't be empty or null in IncludeFile");
                renderer.Write(inclusion.Context.GetRaw());

                return;
            }

            if (!PathUtility.IsRelativePath(inclusion.Context.IncludedFilePath))
            {
                var tag = "ERROR INCLUDE";
                var message = $"Unable to resolve {inclusion.Context.GetRaw()}: Absolute path \"{inclusion.Context.IncludedFilePath}\" is not supported.";
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

            if (!_context.InclusionSet.Contains(includedFilePath))
            {
                string tag = "ERROR INCLUDE";
                string message = $"Unable to resolve {inclusion.Context.GetRaw()}: Circular dependency found in \"{_context.FilePath}\"";
                ExtensionsHelper.GenerateNodeWithCommentWrapper(renderer, tag, message, inclusion.Context.GetRaw(), inclusion.Line);

                return;
            }

            _context.Dependencies.Add(includedFilePath);

            var content = EnvironmentContext.FileAbstractLayer.ReadAllText(includedFilePath.RemoveWorkingFolder());
            var context = new MarkdownContext(
                content,
                _context.BasePath,
                includedFilePath.RemoveWorkingFolder(),
                _context.IsInline,
                _context.InclusionSet.Add(currentFilePath),
                _context.Dependencies,
                _context.EnableSourceInfo,
                _context.Tokens,
                _context.Mvb);

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
