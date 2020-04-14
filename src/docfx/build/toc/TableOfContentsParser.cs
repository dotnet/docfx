// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Markdig.Extensions.Yaml;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsParser
    {
        private readonly Input _input;
        private readonly MarkdownEngine _markdownEngine;

        public TableOfContentsParser(Input input, MarkdownEngine markdownEngine)
        {
            _input = input;
            _markdownEngine = markdownEngine;
        }

        public TableOfContentsNode Parse(FilePath file, List<Error> errors)
        {
            return file.Format switch
            {
                FileFormat.Yaml => Deserialize(_input.ReadYaml(file), errors),
                FileFormat.Json => Deserialize(_input.ReadJson(file), errors),
                FileFormat.Markdown => ParseMarkdown(_input.ReadString(file), file, errors),
                _ => throw new NotSupportedException($"'{file}' is an unknown TOC file"),
            };
        }

        private static TableOfContentsNode Deserialize((List<Error>, JToken) input, List<Error> errors)
        {
            var (inputErrors, token) = input;
            errors.AddRange(inputErrors);

            if (token is JArray tocArray)
            {
                // toc model
                var (toObjectErrors, items) = JsonUtility.ToObject<List<SourceInfo<TableOfContentsNode>>>(tocArray);
                errors.AddRange(toObjectErrors);
                return new TableOfContentsNode { Items = items };
            }
            else if (token is JObject tocObject)
            {
                // toc root model
                var (loadErrors, result) = JsonUtility.ToObject<TableOfContentsNode>(tocObject);
                errors.AddRange(loadErrors);
                return result;
            }
            return new TableOfContentsNode();
        }

        private TableOfContentsNode ParseMarkdown(string content, FilePath file, List<Error> errors)
        {
            var headingBlocks = new List<HeadingBlock>();
            var (markupErrors, ast) = _markdownEngine.Parse(content, MarkdownPipelineType.TocMarkdown);
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
                        errors.Add(Errors.TableOfContents.InvalidTocSyntax(block.ToSourceInfo(file: file)));
                        break;
                }
            }

            using var reader = new StringReader(content);
            return new TableOfContentsNode { Items = BuildTree(errors, file, headingBlocks) };
        }

        private static List<SourceInfo<TableOfContentsNode>> BuildTree(List<Error> errors, FilePath filePath, List<HeadingBlock> blocks)
        {
            if (blocks.Count <= 0)
            {
                return new List<SourceInfo<TableOfContentsNode>>();
            }

            var result = new TableOfContentsNode();
            var stack = new Stack<(int level, TableOfContentsNode item)>();

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
                    errors.Add(Errors.TableOfContents.InvalidTocLevel(block.ToSourceInfo(file: filePath), parent.level, currentLevel));
                }
                else
                {
                    parent.node.Items.Add(currentItem.Value);
                }

                stack.Push((currentLevel, currentItem));
            }

            return result.Items;
        }

        private static SourceInfo<TableOfContentsNode>? GetItem(List<Error> errors, FilePath filePath, HeadingBlock block)
        {
            var source = block.ToSourceInfo(file: filePath);
            var currentItem = new TableOfContentsNode();
            if (block.Inline is null || !block.Inline.Any())
            {
                currentItem.Name = new SourceInfo<string?>(null, source);
                return new SourceInfo<TableOfContentsNode>(currentItem, source);
            }

            if (block.Inline.Count() > 1 && block.Inline.Any(l => l is XrefInline || l is LinkInline))
            {
                errors.Add(Errors.TableOfContents.InvalidTocSyntax(block.ToSourceInfo(file: filePath)));
                return null;
            }

            var xrefLink = block.Inline.FirstOrDefault(l => l is XrefInline);
            if (xrefLink != null && xrefLink is XrefInline xrefInline && !string.IsNullOrEmpty(xrefInline.Href))
            {
                currentItem.Uid = new SourceInfo<string?>(xrefInline.Href, xrefInline.ToSourceInfo(file: filePath));
                return new SourceInfo<TableOfContentsNode>(currentItem, source);
            }

            var link = block.Inline.FirstOrDefault(l => l is LinkInline);
            if (link != null && link is LinkInline linkInline)
            {
                if (!string.IsNullOrEmpty(linkInline.Url))
                {
                    currentItem.Href = new SourceInfo<string?>(linkInline.Url, linkInline.ToSourceInfo(file: filePath));
                }
                if (!string.IsNullOrEmpty(linkInline.Title))
                {
                    currentItem.DisplayName = linkInline.Title;
                }
                currentItem.Name = GetLiteral(errors, filePath, linkInline);
            }

            if (currentItem.Name.Value is null)
            {
                currentItem.Name = GetLiteral(errors, filePath, block.Inline);
            }
            return new SourceInfo<TableOfContentsNode>(currentItem, source);
        }

        private static SourceInfo<string?> GetLiteral(List<Error> errors, FilePath filePath, ContainerInline inline)
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
                    errors.Add(Errors.TableOfContents.InvalidTocSyntax(inline.ToSourceInfo(file: filePath)));
                    return default;
                }
            }

            return new SourceInfo<string?>(result.ToString(), inline.ToSourceInfo(file: filePath));
        }
    }
}
