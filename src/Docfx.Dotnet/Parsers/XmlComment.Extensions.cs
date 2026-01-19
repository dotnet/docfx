// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

#nullable enable

namespace Docfx.Dotnet;

internal partial class XmlComment
{
    /// <summary>
    /// Gets markdown text from XElement.
    /// </summary>
    private static string GetMarkdownText(XElement elem)
    {
        // Gets HTML block tags from tree.
        var nodes = elem.GetBlockTags();

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

// Define file scoped extension methods for GetInnerXml.
static file class GetInnerXmlExtensions
{
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

    private static string RemoveCommonIndent(string text)
    {
        ReadOnlySpan<char> span = text.AsSpan();

        // 1st pass: Compute minimum indent (excluding <pre> blocks)
        bool inPre = false;
        int minIndent = int.MaxValue;

        int pos = 0;
        while (pos < span.Length)
        {
            var line = ReadLine(span, ref pos);

            if (!inPre && !IsWhitespaceLine(line))
            {
                int indent = CountIndent(line);
                if (indent < minIndent)
                    minIndent = indent;
            }

            inPre = UpdatePreFlag(inPre, line);
        }

        if (minIndent == int.MaxValue)
            minIndent = 0;

        // 2nd pass: build result
        var sb = new StringBuilder(text.Length + 8);

        inPre = false;
        pos = 0;

        while (pos < span.Length)
        {
            var line = ReadLine(span, ref pos);

            if (!inPre && line.Length != 0)
            {
                int remove = Math.Min(minIndent, CountIndent(line));
                sb.Append(line.Slice(remove));
            }
            else
            {
                sb.Append(line);
            }

            sb.Append('\n');

            inPre = UpdatePreFlag(inPre, line);
        }

        // Ensure trailing newline
        sb.Append('\n');

        return sb.ToString();
    }

    private static int CountIndent(ReadOnlySpan<char> line)
    {
        int i = 0;
        while (i < line.Length && HelperMethods.IsIndentChar(line[i]))
            i++;
        return i;
    }

    private static bool UpdatePreFlag(bool inPre, ReadOnlySpan<char> line)
    {
        var trimmed = line.Trim();

        // Check start tag (It might contains attribute）
        if (!inPre && trimmed.StartsWith("<pre", StringComparison.OrdinalIgnoreCase))
            inPre = true;

        // Check tag end exits.
        if (inPre && trimmed.EndsWith("</pre>", StringComparison.OrdinalIgnoreCase))
            inPre = false;

        return inPre;
    }

    private static bool IsWhitespaceLine(ReadOnlySpan<char> line)
    {
        foreach (var c in line)
        {
            if (!char.IsWhiteSpace(c))
                return false;
        }
        return true;
    }

    private static ReadOnlySpan<char> ReadLine(ReadOnlySpan<char> text, ref int pos)
    {
        int start = pos;
        while (pos < text.Length && text[pos] != '\n')
            ++pos;

        int length = pos - start;

        // skip '\n'
        if (pos < text.Length && text[pos] == '\n')
            ++pos;

        return text.Slice(start, length);
    }
}

// Define file scoped extension methods for XNode/XElement.
static file class XNodeExtensions
{
    /// <summary>
    /// The whole spacing rule is defined ONLY here.
    /// Key = (left, right)
    /// Value = need empty line between them
    /// </summary>
    private static readonly Dictionary<(NodeKind prev, NodeKind next), bool> NeedEmptyLineRules = new()
    {
        //Block-> *
        [(NodeKind.Block, NodeKind.Other)] = false,
        [(NodeKind.Block, NodeKind.Block)] = false,
        [(NodeKind.Block, NodeKind.Pre)] = true,
        [(NodeKind.Block, NodeKind.Text)] = true,

        // Pre -> *
        [(NodeKind.Pre, NodeKind.Other)] = true,
        [(NodeKind.Pre, NodeKind.Block)] = true,
        [(NodeKind.Pre, NodeKind.Pre)] = false,
        [(NodeKind.Pre, NodeKind.Text)] = true,

        // Other -> *
        [(NodeKind.Other, NodeKind.Block)] = false,
        [(NodeKind.Other, NodeKind.Pre)] = true,
        [(NodeKind.Other, NodeKind.Other)] = false,
        [(NodeKind.Other, NodeKind.Text)] = true,

        // Text -> *
        [(NodeKind.Text, NodeKind.Block)] = true,
        [(NodeKind.Text, NodeKind.Pre)] = true,
        [(NodeKind.Text, NodeKind.Other)] = true,
        [(NodeKind.Text, NodeKind.Text)] = false,
    };

    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
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

    private enum NodeKind
    {
        // XElement
        Block, // HTML element that requires empty line before/after tag.
        Pre,   // <pre> tag. It's handled same as block type. It require additional rules.
        Other, // Other HTML tags

        // XText
        Text,
    }

    private enum Direction
    {
        Before,
        After,
    }

    public static XElement[] GetBlockTags(this XElement elem)
    {
        return elem.Descendants()
                   .Where(e => BlockTags.Contains(e.Name.LocalName))
                   .ToArray();
    }

    public static bool NeedEmptyLineBefore(this XElement node)
        => NeedEmptyLine(node, Direction.Before);

    public static void EnsureEmptyLineBefore(this XElement node)
        => EnsureEmptyLine(node, Direction.Before);

    public static bool NeedEmptyLineAfter(this XElement node)
        => NeedEmptyLine(node, Direction.After);

    public static void EnsureEmptyLineAfter(this XElement node)
        => EnsureEmptyLine(node, Direction.After);

    private static bool NeedEmptyLine(this XElement node, Direction direction)
    {
        // Check whitespace text node.
        XNode? neighborNode = FindNonWhitespaceNeighbor(node, direction);

        if (neighborNode == null)
            return false;

        NodeKind leftKind;
        NodeKind rightKind;

        if (direction == Direction.Before)
        {
            leftKind = GetNodeKind(neighborNode);
            rightKind = GetNodeKind(node);
        }
        else
        {
            leftKind = GetNodeKind(node);
            rightKind = GetNodeKind(neighborNode);
        }

        return NeedEmptyLineRules.TryGetValue((leftKind, rightKind), out var result) && result;
    }

    private static void EnsureEmptyLine(this XNode node, Direction direction)
    {
        var adjacentNode = GetAdjacentNode(node, direction);

        switch (adjacentNode)
        {
            case null:
            case XElement:
                if (direction == Direction.Before)
                    node.AddBeforeSelf(new XText("\n\n"));
                else
                    node.AddAfterSelf(new XText("\n\n"));
                return;

            case XText textNode:

                int count = textNode.CountNewLines(direction, out var insertIndex);

                switch (count)
                {
                    case 0:
                        textNode.Value = textNode.Value.Insert(insertIndex, "\n\n");
                        return;
                    case 1:
                        textNode.Value = textNode.Value.Insert(insertIndex, "\n");
                        return;
                    default:
                        Debug.Assert(textNode.HasEmptyLine(direction));
                        return;
                }

            default:
                return;
        }
    }

    private static NodeKind GetNodeKind(XNode node)
    {
        if (node is not XElement elem)
            return NodeKind.Text;

        if (elem.IsPreTag())
            return NodeKind.Pre;

        if (elem.IsBlockTag())
            return NodeKind.Block;

        return NodeKind.Other;
    }

    private static XNode? GetAdjacentNode(this XNode node, Direction direction)
    {
        return direction == Direction.Before
            ? node.PreviousNode
            : node.NextNode;
    }

    private static XNode? FindNonWhitespaceNeighbor(this XNode node, Direction direction)
    {
        var current = node.GetAdjacentNode(direction);

        while (current != null && current.IsWhitespaceNode())
            current = current.GetAdjacentNode(direction);

        // If node is not found. Use parent instead.
        current ??= node.Parent;

        return current;
    }

    private static bool HasEmptyLine(this XText node, Direction direction)
      => CountNewLines(node, direction, out _) >= 2;

    /// <summary>
    /// Get count of new lines. space and tabs are ignored.
    /// </summary>
    private static int CountNewLines(this XText node, Direction direction, out int insertIndex)
    {
        var span = node.Value.AsSpan();
        int count = 0;

        switch (direction)
        {
            case Direction.Before:
                insertIndex = span.Length;
                for (int i = span.Length - 1; i >= 0; --i)
                {
                    char c = span[i];

                    if (HelperMethods.IsIndentChar(c))
                        continue;

                    if (c != '\n')
                        break;

                    if (count == 0)
                        insertIndex = i + 1;

                    count++;
                }
                return count;

            case Direction.After:
                insertIndex = 0;
                for (int i = 0; i < span.Length; ++i)
                {
                    char c = span[i];

                    if (HelperMethods.IsIndentChar(c))
                        continue;

                    if (c != '\n')
                        break;

                    if (count == 0)
                        insertIndex = i;

                    count++;
                }
                return count;

            default:
                throw new UnreachableException();
        }
    }

    private static bool IsPreTag(this XElement elem)
        => elem.Name.LocalName == "pre";

    private static bool IsBlockTag(this XElement elem)
        => BlockTags.Contains(elem.Name.LocalName);
}

// Define helper methods that are shared between extensions.
static file class HelperMethods
{
    public static bool IsIndentChar(char c)
        => c == ' ' || c == '\t';

    public static bool IsWhitespaceNode(this XNode node)
    {
        if (node is not XText textNode)
            return false;

        return textNode.Value.All(char.IsWhiteSpace);
    }
}
