// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.IO;
    using System.Text.RegularExpressions;

    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class HtmlInclusionBlockRenderer : HtmlObjectRenderer<InclusionBlock>
    {
        private IMarkdownEngine _engine;
        private MarkdownContext _context;
        private MarkdownServiceParameters _parameters;
        private Regex YamlHeaderRegex = new Regex(@"^<yamlheader[^>]*?>[\s\S]*?<\/yamlheader>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public HtmlInclusionBlockRenderer(IMarkdownEngine engine, MarkdownContext context, MarkdownServiceParameters parameters)
        {
            _engine = engine;
            _context = context;
            _parameters = parameters;
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

            var filePath = Path.Combine(_context.BasePath, includedFilePath.RemoveWorkingFolder());
            if (!File.Exists(filePath))
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

            var content = File.ReadAllText(filePath);
            var context = new MarkdownContextBuilder()
                            .WithContext(_context)
                            .WithFilePath(includedFilePath.RemoveWorkingFolder())
                            .WithContent(content)
                            .WithAddingIncludedFile(currentFilePath)
                            .Build();

            _engine.ReportDependency(includedFilePath);
            var result = _engine.Markup(context, _parameters);
            result = SkipYamlHeader(result);

            renderer.Write(result);
        }

        private string SkipYamlHeader(string content)
        {
            return YamlHeaderRegex.Replace(content, "");
        }
    }
}
