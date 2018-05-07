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

        public static ProcessDocumentDelegate GetProcessDocumentDelegate(string filePath)
        {
            return (MarkdownDocument document) =>
            {
                AddSourceInfoInDataEntry(document, filePath);
            };
        }

        /// <summary>
        /// if context.EnableSourceInfo is true: add sourceFile, sourceStartLineNumber, sourceEndLineNumber in each MarkdownObject
        /// </summary>
        /// <param name="markdownObject"></param>
        /// <param name="context"></param>
        private static void AddSourceInfoInDataEntry(MarkdownObject markdownObject, string filePath)
        {
            if (markdownObject == null || filePath == null) return;

            // set linenumber for its children recursively
            if (markdownObject is ContainerBlock containerBlock)
            {
                foreach (var subBlock in containerBlock)
                {
                    AddSourceInfoInDataEntry(subBlock, filePath);
                }
            }
            else if (markdownObject is LeafBlock leafBlock)
            {
                if (leafBlock.Inline != null)
                {
                    foreach (var subInline in leafBlock.Inline)
                    {
                        AddSourceInfoInDataEntry(subInline, filePath);
                    }
                }
            }
            else if (markdownObject is ContainerInline containerInline)
            {
                foreach (var subInline in containerInline)
                {
                    AddSourceInfoInDataEntry(subInline, filePath);
                }
            }

            // set linenumber for this object
            var htmlAttributes = markdownObject.GetAttributes();
            htmlAttributes.AddPropertyIfNotExist("sourceFile", filePath);
            htmlAttributes.AddPropertyIfNotExist("sourceStartLineNumber", markdownObject.Line + 1);
        }
    }
}
