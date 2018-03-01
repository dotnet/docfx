// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

    public class LineNumberExtension
    {
        public const string EnableSourceInfo = "EnableSourceInfo";

        public static ProcessDocumentDelegate GetProcessDocumentDelegate(LineNumberExtensionContext lineNumberContext)
        {
            return (MarkdownDocument document) =>
           {
               AddSourceInfoInDataEntry(document, lineNumberContext);
           };
        }

        /// <summary>
        /// if context.EnableSourceInfo is true: add sourceFile, sourceStartLineNumber, sourceEndLineNumber in each MarkdownObject
        /// </summary>
        /// <param name="markdownObject"></param>
        /// <param name="context"></param>
        private static void AddSourceInfoInDataEntry(MarkdownObject markdownObject, LineNumberExtensionContext lineNumberContext)
        {
            if (markdownObject == null || lineNumberContext == null) return;

            // set linenumber for its children recursively
            if (markdownObject is ContainerBlock containerBlock)
            {
                foreach (var subBlock in containerBlock)
                {
                    AddSourceInfoInDataEntry(subBlock, lineNumberContext);
                }
            }
            else if (markdownObject is LeafBlock leafBlock)
            {
                if (leafBlock.Inline != null)
                {
                    foreach (var subInline in leafBlock.Inline)
                    {
                        AddSourceInfoInDataEntry(subInline, lineNumberContext);
                    }
                }
            }
            else if (markdownObject is ContainerInline containerInline)
            {
                foreach (var subInline in containerInline)
                {
                    AddSourceInfoInDataEntry(subInline, lineNumberContext);
                }
            }

            // set linenumber for this object
            var htmlAttributes = markdownObject.GetAttributes();
            htmlAttributes.AddPropertyIfNotExist("sourceFile", lineNumberContext.FilePath);
            htmlAttributes.AddPropertyIfNotExist("sourceStartLineNumber", markdownObject.Line + 1);
            htmlAttributes.AddPropertyIfNotExist("sourceEndLineNumber", lineNumberContext.GetLineNumber(markdownObject.Span.End, markdownObject.Line) + 1);
        }
    }
}
