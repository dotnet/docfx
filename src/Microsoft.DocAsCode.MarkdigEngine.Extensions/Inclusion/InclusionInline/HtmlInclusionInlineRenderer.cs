// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class HtmlInclusionInlineRenderer : HtmlObjectRenderer<InclusionInline>
    {
        private IMarkdownEngine _engine;
        private MarkdownContext _context;
        private MarkdownServiceParameters _parameters;

        public HtmlInclusionInlineRenderer(IMarkdownEngine engine, MarkdownContext context, MarkdownServiceParameters parameters)
        {
            _engine = engine;
            _context = context;
            _parameters = parameters;
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

            var parents = _context.InclusionSet;
            if (parents != null && parents.Contains(includedFilePath))
            {
                string tag = "ERROR INCLUDE";
                string message = $"Unable to resolve {inclusion.Context.GetRaw()}: Circular dependency found in \"{_context.FilePath}\"";
                ExtensionsHelper.GenerateNodeWithCommentWrapper(renderer, tag, message, inclusion.Context.GetRaw(), inclusion.Line);

                return;
            }

            var content = EnvironmentContext.FileAbstractLayer.ReadAllText(includedFilePath.RemoveWorkingFolder());
            var context = new MarkdownContextBuilder()
                            .WithContext(_context)
                            .WithFilePath(includedFilePath.RemoveWorkingFolder())
                            .WithContent(content)
                            .WithIsInline(true)
                            .WithAddingIncludedFile(currentFilePath)
                            .Build();

            _engine.ReportDependency(includedFilePath);
            // Do not need to check if content is a single paragragh
            // context.IsInline = true will force it into a single paragragh and render with no <p></p>
            var result = _engine.Markup(context, _parameters);
            renderer.Write(result);
        }
    }
}
