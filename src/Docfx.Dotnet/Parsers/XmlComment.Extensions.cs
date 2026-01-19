// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

#nullable enable

namespace Docfx.Dotnet;

internal partial class XmlComment
{
    // List of block tags that are defined by CommonMark
    // https://spec.commonmark.org/0.31.2/#html-blocks
    private static readonly string[] BlockTags =
    {
        "ol",
        "p",
        "table",
        "ul",

        // Recommended XML tags for C# documentation comments
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags
        // Note: Some XML tags(e.g. `<para>`/`<list>`) are pre-processed and converted to HTML tags.
        "example",
        
        // Other tags
        "pre",
    };

    private static readonly Lazy<string> BlockTagsXPath = new(string.Join(" | ", BlockTags.Select(tagName => $".//{tagName}")));

    /// <summary>
    /// Gets markdown text from XElement.
    /// </summary>
    private static string GetMarkdownText(XElement elem)
    {
        // Gets HTML block tags by XPath.
        var nodes = elem.XPathSelectElements(BlockTagsXPath.Value).ToArray();

        // Insert HTML/Markdown separator lines.
        foreach (var node in nodes)
        {
            if (node.NeedEmptyLineBefore())
                node.EnsureEmptyLineBefore();

            if (node.NeedEmptyLineAfter())
                node.EnsureEmptyLineAfter();
        }

        return elem.GetInnerXml();
    }

    private static string GetInnerXml(XElement elem)
        => elem.GetInnerXml();
}

// Define file scoped extension methods.
static file class XElementExtensions
{
    /// <summary>
    /// Gets inner XML text of XElement.
    /// </summary>
    public static string GetInnerXml(this XElement elem)
    {
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment, // Required to write XML partial fragment
            Indent = false,                               // Preserve original indents
            NewLineChars = "\n",                          // Use LF
        });

        var nodes = elem.Nodes().ToArray();
        foreach (var node in nodes)
        {
            node.WriteTo(writer);
        }
        writer.Flush();

        var xml = sw.ToString();

        // Remove shared indents.
        xml = RemoveCommonIndent(xml);

        // Trim beginning spaces/lines if text starts with HTML tag.
        var firstNode = nodes.FirstOrDefault(x => !x.IsWhitespaceNode());
        if (firstNode != null && firstNode.NodeType == XmlNodeType.Element)
            xml = xml.TrimStart();

        // Trim ending spaces/lines if text ends with HTML tag.
        var lastNode = nodes.LastOrDefault(x => !x.IsWhitespaceNode());
        if (lastNode != null && lastNode.NodeType == XmlNodeType.Element)
            xml = xml.TrimEnd();

        return xml;
    }

    public static bool NeedEmptyLineBefore(this XElement node)
    {
        // Case 1: There is a previous node that is non-whitespace.
        if (node.TryGetNonWhitespacePrevNode(out var prevNode))
        {
            return prevNode switch
            {
                // XElement exists on previous nodes.
                XElement prevElem =>
                    node.IsPreTag() && !prevElem.IsPreTag(),

                // XText node exists on previous nodes.
                XText prevText =>
                    !prevText.EndsWithEmptyLine(),

                // Other node types is not expected, and no need to insert empty line.
                _ => false
            };
        }

        // Case 2: There is no previous non-whitespace node
        // Empty Line is not needed except for <pre> tag.
        if (!node.IsPreTag())
            return false;

        // If previous node is XText. Check text ends with empty line.
        if (node.PreviousNode is XText whitespaceNode)
            return !whitespaceNode.EndsWithEmptyLine();

        // Otherwise, empty line is needed when it has a parent node.
        return node.Parent != null;
    }

    public static void EnsureEmptyLineBefore(this XNode node)
    {
        switch (node.PreviousNode)
        {
            case null:
            case XElement:
                node.AddBeforeSelf(new XText("\n\n"));
                return;

            case XText textNode:
                var text = textNode.Value;

                switch (CountTrailingNewLines(text, out var insertIndex))
                {
                    case 0:
                        textNode.Value = text.Insert(insertIndex, "\n\n");
                        return;

                    case 1:
                        textNode.Value = text.Insert(insertIndex, "\n");
                        return;

                    default:
                        // This code path is not expected to be called.
                        // Because it should be filtered by NeedEmptyLineBefore.
                        Debug.Assert(textNode.EndsWithEmptyLine());
                        return;

                }

            default:
                return;
        }
    }

    public static bool NeedEmptyLineAfter(this XElement node)
    {
        // Case 1: There is a next node that is non-whitespace.
        if (node.TryGetNonWhitespaceNextNode(out var nextNode))
        {
            return nextNode switch
            {
                // XElement exists on previous nodes.
                XElement nextElem =>
                    node.IsPreTag() && !nextElem.IsPreTag(),

                // XText node exists on previous nodes.
                XText nextText =>
                    !nextText.StartsWithEmptyLine(),

                // Other node types is not expected, and no need to insert empty line.
                _ => false
            };
        }

        // Case 2: There is no next non-whitespace node
        // Empty Line is not needed except for <pre> tag.
        if (!node.IsPreTag())
            return false;

        // If previous node is XText. Check text ends with empty line.
        if (node.NextNode is XText whitespaceNode)
            return !whitespaceNode.StartsWithEmptyLine();

        var parentNextNode = node.Parent?.NextNode;
        if (parentNextNode is XElement)
            return true;

        if (parentNextNode is XText textNode)
            return !textNode.StartsWithEmptyLine();

        return node.Parent != null;
    }

    public static void EnsureEmptyLineAfter(this XNode node)
    {
        switch (node.NextNode)
        {
            case XElement:
                node.AddAfterSelf(new XText("\n\n"));
                return;

            case XText textNode:
                var textValue = textNode.Value;

                switch (CountLeadingNewLines(textValue, out var insertIndex))
                {
                    case 0:
                        textNode.Value = textValue.Insert(insertIndex, "\n\n");
                        return;

                    case 1:
                        textNode.Value = textValue.Insert(insertIndex, "\n");
                        return;

                    default:
                        // This code path is not expected to be called.
                        // Because it should be filtered by NeedEmptyLineAfter.
                        Debug.Assert(textNode.StartsWithEmptyLine());
                        return;
                }

            default:
                return;
        }
    }


    /// <summary>
    /// Get count of trailing new lines. space and tabs are ignored.
    /// </summary>
    private static int CountTrailingNewLines(ReadOnlySpan<char> span, out int insertIndex)
    {
        insertIndex = span.Length;
        bool insertIndexUpdated = false;
        int count = 0;

        int i = span.Length;
        while (--i >= 0)
        {
            var c = span[i];
            if (IsIndentChar(c))
                continue;

            if (c != '\n')
                return count;

            if (!insertIndexUpdated)
            {
                insertIndexUpdated = true;
                insertIndex = i + 1;
            }
            ++count;
        }

        return count;
    }

    /// <summary>
    /// Get count of leading new lines. space and tabs are ignored.
    /// </summary>
    private static int CountLeadingNewLines(ReadOnlySpan<char> span, out int insertIndex)
    {
        insertIndex = 0;
        bool insertIndexUpdated = false;
        int count = 0;

        for (int i = 0; i < span.Length; ++i)
        {
            var c = span[i];
            if (IsIndentChar(c))
                continue;

            if (c != '\n')
                return count;

            if (!insertIndexUpdated)
            {
                insertIndexUpdated = true;
                insertIndex = i;
            }
            ++count;
        }

        return count;
    }

    private static bool StartsWithEmptyLine(this XNode? node)
    {
        if (node is not XText textNode)
            return false;

        return CountLeadingNewLines(textNode.Value, out _) >= 2;
    }

    private static bool EndsWithEmptyLine(this XNode? node)
    {
        if (node is not XText textNode)
            return false;

        return CountTrailingNewLines(textNode.Value, out _) >= 2;
    }

    private static bool TryGetNonWhitespacePrevNode(this XElement elem, [NotNullWhen(true)] out XNode? result)
    {
        var prev = elem.PreviousNode;
        while (prev is not null && prev.IsWhitespaceNode())
            prev = prev.PreviousNode;

        result = prev;
        return result is not null;
    }

    private static bool TryGetNonWhitespaceNextNode(this XElement elem, [NotNullWhen(true)] out XNode? result)
    {
        var next = elem.NextNode;
        while (next != null && next.IsWhitespaceNode())
            next = next.NextNode;

        result = next;
        return result is not null;
    }

    private static string RemoveCommonIndent(string text)
    {
        var lines = text.Split('\n');

        // 1st pass: Compute minimum indent (excluding <pre> blocks)
        bool inPre = false;
        int minIndent = int.MaxValue;

        foreach (var line in lines)
        {
            if (!inPre && !string.IsNullOrWhiteSpace(line))
            {
                int indent = CountIndent(line);
                minIndent = Math.Min(minIndent, indent);
            }

            UpdatePreFlag(line, ref inPre);
        }

        if (minIndent == int.MaxValue)
            minIndent = 0;

        // 2nd pass: remove common indent
        var sb = new StringBuilder(text.Length);
        inPre = false;

        foreach (var line in lines)
        {
            if (!inPre && line.Length != 0)
            {
                int removeLength = Math.Min(minIndent, CountIndent(line));
                sb.Append(line.AsSpan().Slice(removeLength));
                sb.Append('\n');
            }
            else
            {
                sb.Append(line);
                sb.Append('\n');
            }

            UpdatePreFlag(line, ref inPre);
        }

        // Ensure trailing newline
        sb.Append('\n');

        return sb.ToString();
    }

    private static bool IsWhitespaceNode(this XNode node)
    {
        if (node is not XText textNode)
            return false;

        return textNode.Value.All(char.IsWhiteSpace);
    }

    private static bool IsPreTag(this XNode? node)
        => node is XElement elem && elem.Name == "pre";

    private static int CountIndent(ReadOnlySpan<char> line)
    {
        int i = 0;
        while (i < line.Length && IsIndentChar(line[i]))
            i++;
        return i;
    }

    private static void UpdatePreFlag(ReadOnlySpan<char> line, ref bool inPre)
    {
        var trimmed = line.Trim();

        // Check start tag (It might contains attribute）
        if (!inPre && trimmed.StartsWith("<pre", StringComparison.OrdinalIgnoreCase))
            inPre = true;

        // Check tag end exits.
        if (inPre && trimmed.EndsWith("</pre>", StringComparison.OrdinalIgnoreCase))
            inPre = false;
    }

    private static bool IsIndentChar(char c)
    {
        switch (c)
        {
            case ' ':
            case '\t':
                return true;
            default:
                return false;
        }
    }
}
