// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build;

internal static class DocsValidationExtension
{
    public static MarkdownPipelineBuilder UseDocsValidation(
        this MarkdownPipelineBuilder builder,
        MarkdownEngine markdownEngine,
        ContentValidator contentValidator,
        Func<MonikerList> getFileLevelMonikers,
        Func<string?> getCanonicalVersion)
    {
        return builder.Use(document =>
        {
            var currentFile = ((SourceInfo)InclusionContext.File).File;

            var documentNodes = new List<ContentNode>();
            var codeBlockNodes = new List<(bool isInclude, CodeBlockNode codeBlockItem)>();
            var tableNodes = new List<TableNode>();

            var canonicalVersion = getCanonicalVersion();
            var fileLevelMoniker = getFileLevelMonikers();

            var zonePivotUsages = new List<SourceInfo<string>>();

            document.Visit(node =>
            {
                // Skip leaf triple colon nodes
                if (node is TripleColonBlock tripleColon)
                {
                    if (tripleColon.Extension is ImageExtension || tripleColon.Extension is VideoExtension || tripleColon.Extension is CodeExtension)
                    {
                        return true;
                    }

                    // Build zones for validation
                    BuildZonePivotUsages(tripleColon, zonePivotUsages);
                }

                var isCanonicalVersion = IsCanonicalVersion(canonicalVersion, fileLevelMoniker, node.GetZoneLevelMonikers());

                BuildHeadingNodes(node, markdownEngine, documentNodes, isCanonicalVersion);

                BuildCodeBlockNodes(node, codeBlockNodes, isCanonicalVersion);

                BuildTableNodes(node, tableNodes, isCanonicalVersion);

                return false;
            });

            contentValidator.ValidateHeadings(currentFile, documentNodes);
            contentValidator.ValidateZonePivots(currentFile, zonePivotUsages);

            foreach (var (_, codeBlockItem) in codeBlockNodes)
            {
                contentValidator.ValidateCodeBlock(currentFile, codeBlockItem);
            }

            foreach (var tableItem in tableNodes)
            {
                contentValidator.ValidateTable(currentFile, tableItem);
            }
        });
    }

    private static void BuildHeadingNodes(
        MarkdownObject node,
        MarkdownEngine markdownEngine,
        List<ContentNode> documentNodes,
        bool isCanonicalVersion)
    {
        ContentNode? documentNode = null;

        switch (node)
        {
            case HeadingBlock headingBlock:
                var headingNode = CreateValidationNode<HeadingNode>(isCanonicalVersion, headingBlock) with
                {
                    Level = headingBlock.Level,
                    Content = GetHeadingContent(headingBlock), // used for reporting
                    HeadingChar = headingBlock.HeaderChar,
                    RenderedPlainText = markdownEngine.ToPlainText(headingBlock), // used for validation
                    IsVisible = MarkdigUtility.IsVisible(headingBlock),
                };
                documentNode = headingNode;
                break;

            case LeafBlock leafBlock:
                var contentNode = CreateValidationNode<ContentNode>(isCanonicalVersion, leafBlock) with
                {
                    IsVisible = MarkdigUtility.IsVisible(leafBlock),
                };
                documentNode = contentNode;
                break;
        }

        if (documentNode != null)
        {
            documentNodes.Add(documentNode);
        }
    }

    private static void BuildCodeBlockNodes(
        MarkdownObject node,
        List<(bool IsInclude, CodeBlockNode codeBlockItem)> codeBlockItemList,
        bool isCanonicalVersion)
    {
        CodeBlockNode? codeBlockItem = null;

        switch (node)
        {
            case FencedCodeBlock fencedCodeBlock:
                codeBlockItem = CreateValidationNode<CodeBlockNode>(isCanonicalVersion, node) with
                {
                    Type = CodeBlockTypeEnum.FencedCodeBlock,
                    Info = fencedCodeBlock.Info,
                    Arguments = fencedCodeBlock.Arguments,
                    IsOpen = fencedCodeBlock.IsOpen,
                    LineCount = GetFencedCodeBlockNetLineCount(fencedCodeBlock),
                };
                break;

            case YamlFrontMatterBlock:
                break;

            case CodeBlock codeBlock:
                codeBlockItem = CreateValidationNode<CodeBlockNode>(isCanonicalVersion, codeBlock) with
                {
                    Type = CodeBlockTypeEnum.CodeBlock,
                };
                break;

            default:
                break;
        }

        if (codeBlockItem != null)
        {
            codeBlockItemList.Add((node.IsInclude(), codeBlockItem));
        }
    }

    private static void BuildZonePivotUsages(TripleColonBlock tripleColon, List<SourceInfo<string>> usages)
    {
        if (tripleColon.Extension is ZoneExtension && tripleColon.Attributes.TryGetValue("pivot", out var pivotId))
        {
            usages.AddRange(pivotId.Split(",").Select(p => new SourceInfo<string>(p.Trim(), tripleColon.GetSourceInfo())));
        }
    }

    private static int GetFencedCodeBlockNetLineCount(FencedCodeBlock fencedCodeBlock)
    {
        var netLineCount = 0;

        for (var i = 0; i < fencedCodeBlock.Lines.Count; i++)
        {
            var temSlice = fencedCodeBlock.Lines.Lines[i].Slice;
            temSlice.Trim();

            if (!temSlice.IsEmpty)
            {
                netLineCount++;
            }
        }

        return netLineCount;
    }

    private static bool IsCanonicalVersion(string? canonicalVersion, MonikerList fileLevelMonikerList, MonikerList zoneLevelMonikerList)
    {
        if (zoneLevelMonikerList.HasMonikers)
        {
            return zoneLevelMonikerList.IsCanonicalVersion(canonicalVersion);
        }

        return fileLevelMonikerList.IsCanonicalVersion(canonicalVersion);
    }

    private static T CreateValidationNode<T>(bool isCanonicalVersion, MarkdownObject markdownNode)
        where T : ValidationNode, new()
    {
        return new T()
        {
            IsCanonicalVersion = isCanonicalVersion,
            ParentSourceInfoList = markdownNode.GetInclusionStack(),
            Zone = markdownNode.GetZone(),
            Monikers = markdownNode.GetZoneLevelMonikers().ToList(),
            ZonePivots = markdownNode.GetZonePivots(),
            SourceInfo = markdownNode.GetSourceInfo(),
            TabbedConceptualHeader = markdownNode.GetTabId(),
        };
    }

    private static string GetHeadingContent(HeadingBlock headingBlock)
    {
        if (headingBlock.Inline is null || !headingBlock.Inline.Any())
        {
            return "";
        }

        return GetContainerInlineContent(headingBlock.Inline);
        static string GetContainerInlineContent(ContainerInline containerInline)
        {
            var content = new StringBuilder();
            var child = containerInline.FirstChild;
            while (child != null)
            {
                if (child is LiteralInline childLiteralInline)
                {
                    content.Append(childLiteralInline.Content.Text, childLiteralInline.Content.Start, childLiteralInline.Content.Length);
                }

                if (child is HtmlInline childHtmlInline)
                {
                    content.Append(childHtmlInline.Tag);
                }

                if (child is ContainerInline childContainerInline)
                {
                    content.Append(GetContainerInlineContent(childContainerInline));
                }

                child = child.NextSibling;
            }

            return content.ToString();
        }
    }

    private static void BuildTableNodes(MarkdownObject node, List<TableNode> tableNodes, bool isCanonicalVersion)
    {
        TableNode? tableNode = null;
        switch (node)
        {
            case Table table:
                var parsedNode = TryParseTable(table);
                tableNode = CreateValidationNode<TableNode>(isCanonicalVersion, node) with
                {
                    RowHeaders = parsedNode.RowHeaders,
                    ColumnHeaders = parsedNode.ColumnHeaders,
                    IsSuccessParsed = parsedNode.IsSuccessParsed,
                };
                break;
            case ParagraphBlock paragraphBlock:
                if (TryDetectTable(paragraphBlock))
                {
                    tableNode = CreateValidationNode<TableNode>(isCanonicalVersion, node) with { IsSuccessParsed = false };
                }
                break;
            default:
                break;
        }
        if (tableNode != null)
        {
            tableNodes.Add(tableNode);
        }
    }

    private static TableNode TryParseTable(Table table)
    {
        var columnHeaders = new List<TableCellNode>();
        var rowHeaders = new List<TableCellNode>();
        var columnHeaderRow = (TableRow)table.First();
        foreach (var cell in columnHeaderRow.Cast<TableCell>())
        {
            columnHeaders.Add(ParseCell(cell));
        }
        foreach (var row in table.Cast<TableRow>())
        {
            rowHeaders.Add(ParseCell((TableCell)row.First()));
        }
        return new TableNode
        {
            ColumnHeaders = columnHeaders.ToArray(),
            RowHeaders = rowHeaders.ToArray(),
            IsSuccessParsed = true,
        };
    }

    private static TableCellNode ParseCell(TableCell cell)
    {
        var block = cell.LastChild;
        var isVisible = MarkdigUtility.IsVisible(block);
        var isEmphasis = false;
        MarkdigUtility.Visit(block, node =>
        {
            var innerEmphasis = false;
            if (node is ParagraphBlock paragraphBlock)
            {
                innerEmphasis = paragraphBlock.Inline.FindDescendants<EmphasisInline>().Any(x => x.DelimiterCount >= 2);
            }
            return isEmphasis = isEmphasis || innerEmphasis;
        });
        var cellNode = new TableCellNode
        {
            IsVisible = isVisible,
            IsEmphasis = isEmphasis,
        };
        return cellNode;
    }

    private static bool TryDetectTable(ParagraphBlock paragraph)
    {
        if (!paragraph.Inline.FindDescendants<LineBreakInline>().Any())
        {
            return false;
        }

        var tableDelimiterExistLine = 0;
        var tableHeaderExist = false;

        var inlines = new List<Inline>();
        var lineFinish = false;
        var child = paragraph.Inline.LastChild;
        var stack = new Stack<Inline>();
        while (child != null)
        {
            stack.Push(child);
            child = child.PreviousSibling;
        }
        var pipeDelimiterCount = new List<int>();
        while (stack.Count > 0)
        {
            child = stack.Pop();
            switch (child)
            {
                case ContainerInline containerInline:
                    if (containerInline is PipeTableDelimiterInline)
                    {
                        inlines.Add(containerInline);
                    }
                    child = containerInline.LastChild;
                    while (child != null)
                    {
                        stack.Push(child);
                        child = child.PreviousSibling;
                    }
                    break;
                case LineBreakInline:
                    lineFinish = true;
                    break;
                default:
                    inlines.Add(child);
                    break;
            }
            if (lineFinish || stack.Count == 0)
            {
                var totalText = new StringBuilder();
                foreach (var line in inlines)
                {
                    switch (line)
                    {
                        case PipeTableDelimiterInline:
                            totalText.Append('|');
                            break;
                        case LiteralInline literalInline:
                            var text = literalInline.Content.Text.Substring(literalInline.Content.Start, literalInline.Content.Length);
                            totalText.Append(text);
                            break;
                        default:
                            break;
                    }
                }
                var regex = new Regex(@"^[|:-]*$");
                var lineText = totalText.ToString();
                tableHeaderExist = tableHeaderExist || regex.IsMatch(lineText);
                if (lineText.Contains('|'))
                {
                    pipeDelimiterCount.Add(lineText.Count(x => x == '|'));
                    tableDelimiterExistLine++;
                }
                else
                {
                    pipeDelimiterCount.Add(0);
                }
                if (tableDelimiterExistLine >= 2 && tableHeaderExist)
                {
                    return true;
                }
                inlines.Clear();
                lineFinish = false;
            }
        }
        if (tableDelimiterExistLine >= 2 && pipeDelimiterCount.TrueForAll(x => x >= 2))
        {
            return true;
        }
        return false;
    }
}
