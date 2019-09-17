// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Markdig.Extensions.Yaml;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownTocMarkup
    {
        public static (List<Error> errors, TableOfContentsModel model) Parse(string tocContent, Document file)
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
                        errors.Add(Errors.InvalidTocSyntax(block.ToSourceInfo(file: file)));
                        break;
                }
            }

            using (var reader = new StringReader(tocContent))
            {
                var (metaErrors, metadata) = ExtractYamlHeader.Extract(reader, file);
                errors.AddRange(metaErrors);

                var (validationErrors, tocMetadata) = JsonUtility.ToObject<TableOfContentsMetadata>(metadata);
                errors.AddRange(validationErrors);

                var items = BuildTree(errors, file, headingBlocks);

                var tocModel = new TableOfContentsModel { Metadata = tocMetadata, Items = items };

                return (errors, tocModel);
            }
        }

        private static List<TableOfContentsItem> BuildTree(List<Error> errors, Document filePath, List<HeadingBlock> blocks)
        {
            if (blocks.Count <= 0)
            {
                return new List<TableOfContentsItem>();
            }

            var result = new TableOfContentsItem();
            var stack = new Stack<(int level, TableOfContentsItem item)>();

            // Level of root node is determined by its first child
            var parent = (level: blocks[0].Level - 1, node: result);
            stack.Push(parent);

            foreach (var block in blocks)
            {
                var currentLevel = block.Level;
                var currentItem = GetItem(errors, filePath, block);
                if (currentItem == null)
                {
                    continue;
                }

                while (stack.TryPeek(out parent) && parent.level >= currentLevel)
                {
                    stack.Pop();
                }

                if (parent.node is null || currentLevel != parent.level + 1)
                {
                    errors.Add(Errors.InvalidTocLevel(block.ToSourceInfo(file: filePath), parent.level, currentLevel));
                }
                else
                {
                    parent.node.Items.Add(currentItem);
                }

                stack.Push((currentLevel, currentItem));
            }

            return result.Items;
        }

        private static TableOfContentsItem GetItem(List<Error> errors, Document filePath, HeadingBlock block)
        {
            var currentItem = new TableOfContentsItem();
            if (block.Inline is null || !block.Inline.Any())
            {
                currentItem.Name = new SourceInfo<string>(null, block.ToSourceInfo(file: filePath));
                return currentItem;
            }

            if (block.Inline.Count() > 1 && block.Inline.Any(l => l is XrefInline || l is LinkInline))
            {
                errors.Add(Errors.InvalidTocSyntax(block.ToSourceInfo(file: filePath)));
                return null;
            }

            var xrefLink = block.Inline.FirstOrDefault(l => l is XrefInline);
            if (xrefLink != null && xrefLink is XrefInline xrefInline && !string.IsNullOrEmpty(xrefInline.Href))
            {
                currentItem.Uid = new SourceInfo<string>(xrefInline.Href, xrefInline.ToSourceInfo(file: filePath));
                return currentItem;
            }

            var link = block.Inline.FirstOrDefault(l => l is LinkInline);
            if (link != null && link is LinkInline linkInline)
            {
                if (!string.IsNullOrEmpty(linkInline.Url))
                {
                    currentItem.Href = new SourceInfo<string>(linkInline.Url, linkInline.ToSourceInfo(file: filePath));
                }
                if (!string.IsNullOrEmpty(linkInline.Title))
                    currentItem.DisplayName = linkInline.Title;

                currentItem.Name = GetLiteral(errors, filePath, linkInline);
            }

            if (currentItem.Name.Value is null)
            {
                currentItem.Name = GetLiteral(errors, filePath, block.Inline);
            }

            return currentItem;
        }

        private static SourceInfo<string> GetLiteral(List<Error> errors, Document filePath, ContainerInline inline)
        {
            var result = new StringBuilder();
            var child = inline.FirstChild;

            while (child != null)
            {
                if (child is LiteralInline literal)
                {
                    var content = literal.Content;
                    result.Append(content.Text, content.Start, content.Length);
                    child = child.NextSibling;
                }
                else if (child is XrefInline xref)
                {
                    foreach (var pair in xref.GetAttributes().Properties)
                    {
                        if (pair.Key == "data-raw-source")
                        {
                            result.Append(pair.Value);
                            break;
                        }
                    }
                    child = child.NextSibling;
                }
                else
                {
                    errors.Add(Errors.InvalidTocSyntax(inline.ToSourceInfo(file: filePath)));
                    return default;
                }
            }

            return new SourceInfo<string>(result.ToString(), inline.ToSourceInfo(file: filePath));
        }
    }
}
