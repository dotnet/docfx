// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownTocMarkup
    {
        public static (List<Error> errors, TableOfContentsModel model) LoadMdTocModel(string tocContent, Document file, Context context)
        {
            var errors = new List<Error>();
            var headingBlocks = new List<HeadingBlock>();
            var (markupErrors, ast) = MarkdownUtility.Parse(tocContent, MarkdownPipelineType.TocMarkdown);
            errors.AddRange(markupErrors);

            foreach (var block in ast)
            {
                switch (block)
                {
                    case HeadingBlock headingBlock:
                        headingBlocks.Add(headingBlock);
                        break;
                    case YamlFrontMatterBlock _:
                    case HtmlBlock htmlBlock when htmlBlock.Type == HtmlBlockType.Comment:
                        break;
                    default:
                        errors.Add(Errors.InvalidTocSyntax(new SourceInfo(file.FilePath, block.Line, block.Column), tocContent.Substring(block.Span.Start, block.Span.Length)));
                        break;
                }
            }

            var (metaErrors, metadata) = ExtractYamlHeader.Extract(file, context);
            errors.AddRange(metaErrors);

            var (validationErrors, tocMetadata) = JsonUtility.ToObject<TableOfContentsMetadata>(metadata);
            errors.AddRange(validationErrors);

            var tocModel = new TableOfContentsModel { Metadata = tocMetadata };

            try
            {
                tocModel.Items = ConvertTo(tocContent, file.FilePath, headingBlocks.ToArray(), errors).children;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.Add(dex.Error);
            }

            return (errors, tocModel);
        }

        private static (List<TableOfContentsItem> children, int count) ConvertTo(string tocContent, string filePath, HeadingBlock[] headingBlocks, List<Error> errors, int startIndex = 0)
        {
            if (headingBlocks.Length == 0)
            {
                return (new List<TableOfContentsItem>(), 0);
            }

            Debug.Assert(startIndex < headingBlocks.Length);

            int i = startIndex;
            var items = new List<TableOfContentsItem>();
            var childrenCount = 0;
            do
            {
                var item = GetItem(headingBlocks[i]);
                items.Add(item);
                var currentLevel = headingBlocks[i].Level;
                if (i + 1 < headingBlocks.Length && headingBlocks[i + 1].Level > currentLevel)
                {
                    if (headingBlocks[i + 1].Level - currentLevel > 1)
                    {
                        throw Errors.InvalidTocLevel(filePath, currentLevel, headingBlocks[i + 1].Level).ToException();
                    }

                    var (children, count) = ConvertTo(tocContent, filePath, headingBlocks, errors, i + 1);
                    item.Items = children;
                    i += count;
                    childrenCount += count;
                }

                if (i + 1 < headingBlocks.Length && headingBlocks[i + 1].Level < currentLevel)
                {
                    break;
                }
            }
            while (++i < headingBlocks.Length);

            return (items, items.Count + childrenCount);

            TableOfContentsItem GetItem(HeadingBlock block)
            {
                var currentItem = new TableOfContentsItem();
                if (block.Inline is null || !block.Inline.Any())
                {
                    errors.Add(Errors.MissingTocHead(new SourceInfo(filePath, block.Line, block.Column)));
                    return currentItem;
                }

                if (block.Inline.Count() > 1 && block.Inline.Any(l => l is XrefInline || l is LinkInline))
                {
                    errors.Add(Errors.InvalidTocSyntax(new SourceInfo(filePath, block.Line, block.Column), tocContent.Substring(block.Span.Start, block.Span.Length), "multiple inlines in one heading block is not allowed"));
                    return currentItem;
                }

                var xrefLink = block.Inline.FirstOrDefault(l => l is XrefInline);
                if (xrefLink != null && xrefLink is XrefInline xrefInline && !string.IsNullOrEmpty(xrefInline.Href))
                {
                    currentItem.Uid = new SourceInfo<string>(xrefInline.Href, new SourceInfo(filePath, block.Line, block.Column));
                    return currentItem;
                }

                var link = block.Inline.FirstOrDefault(l => l is LinkInline);
                if (link != null && link is LinkInline linkInline)
                {
                    if (!string.IsNullOrEmpty(linkInline.Url))
                    {
                        // TODO: check why line starts from 0
                        // TODO: check why column info is missing
                        currentItem.Href = new SourceInfo<string>(linkInline.Url, new SourceInfo(filePath, block.Line, block.Column));
                    }
                    if (!string.IsNullOrEmpty(linkInline.Title))
                        currentItem.DisplayName = linkInline.Title;

                    currentItem.Name = GetLiteral(linkInline);
                }

                currentItem.Name = currentItem.Name ?? GetLiteral(block.Inline);

                return currentItem;
            }

            string GetLiteral(ContainerInline inline)
            {
                var result = new StringBuilder();
                var child = inline.FirstChild;

                while (child != null)
                {
                    if (!(child is LiteralInline literal))
                    {
                        errors.Add(Errors.InvalidTocSyntax(new SourceInfo(filePath, inline.Line, inline.Column)));
                        return null;
                    }

                    var content = literal.Content;
                    result.Append(content.Text, content.Start, content.Length);
                    child = child.NextSibling;
                }

                return result.ToString();
            }
        }
    }
}
