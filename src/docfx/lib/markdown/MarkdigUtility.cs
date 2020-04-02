// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Markdig;
using Markdig.Extensions.Emoji;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdigUtility
    {
        private static readonly IReadOnlyDictionary<Type, string> s_markdownElementTypeMapping = new Dictionary<Type, string>()
        {
            { typeof(ThematicBreakBlock), "ThematicBreak" },
            { typeof(CodeBlock), "IndentedCode" },
            { typeof(FencedCodeBlock), "FencedCode" },
            { typeof(HtmlBlock), "HTML" },
            { typeof(LinkReferenceDefinition), "LinkReferenceDefinition" },
            { typeof(ParagraphBlock), "Paragraph" },
            { typeof(BlankLineBlock), "BlankLine" },
            { typeof(ListBlock), "List" },
            { typeof(CodeSnippet), "CodeSnippet" },
            { typeof(Table), "Table" },
            { typeof(TabGroupBlock), "TabbedContent" },
            { typeof(MonikerRangeBlock), "MonikerRange" },
            { typeof(RowBlock), "Row" },
            { typeof(NestedColumnBlock), "NestedColumn" },
            { typeof(YamlFrontMatterBlock), "YamlHeader" },
            { typeof(InclusionBlock), "IncludeFile" },
            { typeof(InclusionInline), "IncludeFile" },
            { typeof(HtmlEntityInline), "HTMLEntity" },
            { typeof(CodeInline), "CodeSpan" },
            { typeof(AutolinkInline), "Autolink" },
            { typeof(HtmlInline), "RawHTML" },
            { typeof(XrefInline), "Xref" },
            { typeof(EmojiInline), "Emoji" },
            { typeof(NolocInline), "Noloc" },
        };

        public static SourceInfo? ToSourceInfo(this MarkdownObject obj, int? line = null, FilePath? file = null, int columnOffset = 0)
        {
            var path = file ?? (InclusionContext.File as Document)?.FilePath;
            if (path is null)
                return default;

            // Line info in markdown object is zero based, turn it into one based.
            if (obj != null)
                return new SourceInfo(path, obj.Line + 1, obj.Column + columnOffset + 1);

            if (line != null)
                return new SourceInfo(path, line.Value + 1, 0);

            return default;
        }

        /// <summary>
        /// Traverse the markdown object graph, returns true to skip the current node.
        /// </summary>
        public static void Visit(this MarkdownObject obj, Func<MarkdownObject, bool> action)
        {
            if (obj is null)
                return;

            if (action(obj))
                return;

            if (obj is ContainerBlock block)
            {
                foreach (var child in block)
                {
                    Visit(child, action);
                }
            }
            else if (obj is ContainerInline inline)
            {
                foreach (var child in inline)
                {
                    Visit(child, action);
                }
            }
            else if (obj is LeafBlock leaf)
            {
                Visit(leaf.Inline, action);
            }
        }

        /// <summary>
        /// Traverses the markdown object graph and replace each node with another node,
        /// If <paramref name="action"/> returns null, remove the node from the graph.
        /// </summary>
        public static MarkdownObject Replace(this MarkdownObject obj, Func<MarkdownObject, MarkdownObject> action)
        {
            obj = action(obj);

            if (obj is ContainerBlock block)
            {
                for (var i = 0; i < block.Count; i++)
                {
                    var replacement = (Block)Replace(block[i], action);
                    if (replacement != block[i])
                    {
                        block.RemoveAt(i--);
                        if (replacement != null)
                        {
                            block.Insert(i, replacement);
                        }
                    }
                }
            }
            else if (obj is ContainerInline inline)
            {
                foreach (var child in inline)
                {
                    var replacement = Replace(child, action);
                    if (replacement is null)
                    {
                        child.Remove();
                    }
                    else if (replacement != child)
                    {
                        child.ReplaceBy((Inline)replacement);
                    }
                }
            }
            else if (obj is LeafBlock leaf)
            {
                leaf.Inline = (ContainerInline)Replace(leaf.Inline, action);
            }

            return obj;
        }

        public static MarkdownPipelineBuilder Use(this MarkdownPipelineBuilder builder, ProcessDocumentDelegate documentProcessed)
        {
            builder.Extensions.Add(new DelegatingExtension(pipeline => pipeline.DocumentProcessed += documentProcessed));
            return builder;
        }

        public static string? GetElementType(MarkdownObject node)
        {
            if (node is null)
            {
                return default;
            }

            string? elementType = s_markdownElementTypeMapping.GetValueOrDefault(node.GetType(), null);
            if (elementType is null)
            {
                if (node is HeadingBlock heading)
                {
                    elementType = heading.HeaderChar == '#' ? "ATXHeading" : "SetextHeading";
                }
                else if (node is QuoteSectionNoteBlock quoteSectionNote)
                {
                    switch (quoteSectionNote.QuoteType)
                    {
                        case QuoteSectionNoteType.DFMNote:
                            elementType = "Note";
                            break;
                        case QuoteSectionNoteType.DFMSection:
                            elementType = "SectionDefinition";
                            break;
                        case QuoteSectionNoteType.DFMVideo:
                            elementType = "Video";
                            break;
                        case QuoteSectionNoteType.MarkdownQuote:
                            elementType = "BlockQuote";
                            break;
                    }
                }
                else if (node is TripleColonBlock tripleColon)
                {
                    var extensionName = StringUtility.UpperCaseFirstChar(tripleColon.Extension.Name);
                    elementType = $"TripleColon{extensionName}";
                }
                else if (node is LiteralInline literal)
                {
                    elementType = literal.IsFirstCharacterEscaped ? "BlackslashEscape" : "TextualContent";
                }
                else if (node is EmphasisInline emphasis)
                {
                    elementType = emphasis.DelimiterCount == 2 ? "StrongEmphasis" : "Emphasis";
                }
                else if (node is LinkInline link)
                {
                    if (link.IsImage)
                    {
                        elementType = "Image";
                    }
                    else if (link.IsAutoLink)
                    {
                        elementType = "Autolink";
                    }
                    else
                    {
                        elementType = "Link";
                    }
                }
                else if (node is LineBreakInline linkBreak)
                {
                    elementType = linkBreak.IsHard ? "HardLineBreak" : "SoftLineBreak";
                }
            }
            return elementType;
        }

        public static string? GetTokenType(MarkdownObject node)
        {
            if (node is null)
            {
                return default;
            }

            var tokenType = node.GetType().Name;
            if (node is HtmlBlock html)
            {
                tokenType += $"-{html.Type}";
            }
            else if (node is QuoteSectionNoteBlock quoteSectionNote)
            {
                tokenType += $"-{quoteSectionNote.QuoteType}";
                if (quoteSectionNote.QuoteType == QuoteSectionNoteType.DFMNote)
                {
                    tokenType += $"-{StringUtility.UpperCaseFirstChar(quoteSectionNote.NoteTypeString)}";
                }
            }
            else if (node is TripleColonBlock tripleColon)
            {
                var extensionName = StringUtility.UpperCaseFirstChar(tripleColon.Extension.Name);
                tokenType += $"-{extensionName}";
            }
            return tokenType;
        }

        private class DelegatingExtension : IMarkdownExtension
        {
            private readonly Action<MarkdownPipelineBuilder> _setupPipeline;

            public DelegatingExtension(Action<MarkdownPipelineBuilder> setupPipeline)
            {
                _setupPipeline = setupPipeline;
            }

            public void Setup(MarkdownPipelineBuilder pipeline) => _setupPipeline?.Invoke(pipeline);

            public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
            {
            }
        }
    }
}
