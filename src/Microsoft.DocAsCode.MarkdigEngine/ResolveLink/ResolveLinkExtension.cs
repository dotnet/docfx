// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using Microsoft.DocAsCode.Common;

    public class ResolveLinkExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.DocumentProcessed += UpdateLinks;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        private static void UpdateLinks(MarkdownObject markdownObject)
        {
            if (markdownObject == null) return;

            if (markdownObject is ContainerBlock containerBlock)
            {
                foreach (var subBlock in containerBlock)
                {
                    UpdateLinks(subBlock);
                }
            }
            else if (markdownObject is LeafBlock leafBlock)
            {
                if (leafBlock.Inline != null)
                {
                    foreach (var subInline in leafBlock.Inline)
                    {
                        UpdateLinks(subInline);
                    }
                }
            }
            else if (markdownObject is ContainerInline containerInline)
            {
                foreach (var subInline in containerInline)
                {
                    UpdateLinks(subInline);
                }

                if (markdownObject is LinkInline linkInline && !linkInline.IsAutoLink)
                {
                    linkInline.GetDynamicUrl = () => GetLink(linkInline.Url, InclusionContext.File, InclusionContext.RootFile, linkInline);
                }
            }
        }

        private static string GetLink(string path, object relativeTo, object resultRelativeTo, MarkdownObject origin)
        {
            if (InclusionContext.IsInclude && RelativePath.IsRelativePath(path) && PathUtility.IsRelativePath(path) && !RelativePath.IsPathFromWorkingFolder(path) && !path.StartsWith("#", StringComparison.Ordinal))
            {
                return ((RelativePath)relativeTo + (RelativePath)path).GetPathFromWorkingFolder();
            }
            return path;
        }
    }
}
