// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.TableOfContents;

public static partial class MarkdownTocReader
{
    private const string ContinuableCharacters = ".,;:!?~";
    private const string StopCharacters = @"\s\""\'<>";
    private const string XrefAutoLinkRegexString = "(<xref:([^ >]+)>)";
    private const string XrefAutoLinkRegexWithQuoteString = @"<xref:(['""])(\s*?\S+?[\s\S]*?)\1>";
    private const string XrefShortcutRegexWithQuoteString = @"@(?:(['""])(?<uid>\s*?\S+?[\s\S]*?)\1)";
    private const string XrefShortcutRegexString = $"@(?<uid>[a-zA-Z](?:[{ContinuableCharacters}]?[^{StopCharacters}{ContinuableCharacters}])*)";

    public static List<TocItemViewModel> LoadToc(string tocContent, string filePath)
    {
        ParseState state = new InitialState(filePath);
        var rules = new ParseRule[]
        {
            new TopicTocParseRule(),
            new ExternalLinkTocParseRule(),
            new TopicXrefAutoLinkTocParseRule(),
            new TopicXrefShortcutTocParseRule(),
            new ContainerParseRule(),
            new CommentParseRule(),
            new WhitespaceParseRule(),
        };
        var content = tocContent.Replace("\r\n", "\n").Replace("\r", "\n");
        int lineNumber = 1;
        while (content.Length > 0)
        {
            state = state.ApplyRules(rules, ref content, ref lineNumber);
        }
        return state.Root;
    }

    internal abstract class ParseState
    {
        public abstract int Level { get; }
        public abstract Stack<TocItemViewModel> Parents { get; }
        public abstract List<TocItemViewModel> Root { get; }
        public abstract string FilePath { get; }

        public virtual ParseState ApplyRules(ParseRule[] rules, ref string input, ref int lineNumber)
        {
            foreach (var rule in rules)
            {
                var m = rule.Match(input);
                if (m.Success)
                {
                    input = input.Substring(m.Length);
                    lineNumber += m.Value.Count(ch => ch == '\n');
                    return rule.Apply(this, m);
                }
            }
            var message = string.Join(Environment.NewLine, input.Split('\n').Take(3));
            return new ErrorState(this, Level, $"Unknown syntax at line {lineNumber}:{Environment.NewLine}{message}");
        }
    }

    internal sealed class InitialState : ParseState
    {
        public InitialState(string filePath)
        {
            Parents = new Stack<TocItemViewModel>();
            Root = [];
            FilePath = filePath;
        }
        public override int Level => 0;
        public override Stack<TocItemViewModel> Parents { get; }
        public override string FilePath { get; }
        public override List<TocItemViewModel> Root { get; }
    }

    internal sealed class NodeState : ParseState
    {
        public NodeState(ParseState state, int level)
        {
            Level = level;
            Parents = state.Parents;
            Root = state.Root;
            FilePath = state.FilePath;
        }
        public override int Level { get; }
        public override Stack<TocItemViewModel> Parents { get; }
        public override List<TocItemViewModel> Root { get; }
        public override string FilePath { get; }
    }

    internal sealed class ErrorState : ParseState
    {
        public ErrorState(ParseState state, int level, string message)
        {
            Level = level;
            Parents = state.Parents;
            Root = state.Root;
            FilePath = state.FilePath;
            Message = message;
        }
        public string Message { get; }
        public override int Level { get; }
        public override Stack<TocItemViewModel> Parents { get; }
        public override List<TocItemViewModel> Root { get; }
        public override string FilePath { get; }
        public override ParseState ApplyRules(ParseRule[] rules, ref string input, ref int lineNumber)
        {
            var message = $"Invalid toc file: {FilePath}, Details: {Message}";
            Logger.LogError(message, code: ErrorCodes.Toc.InvalidMarkdownToc);
            throw new DocumentException(message);
        }
    }

    internal abstract class ParseRule
    {
        public abstract Match Match(string text);

        public abstract ParseState Apply(ParseState state, Match match);

        protected ParseState ApplyCore(ParseState state, int level, string text, string href, string uid = null, string displayText = null)
        {
            if (level > state.Level + 1)
            {
                return new ErrorState(state, level, $"Skip level is not allowed. Toc content: {text}");
            }

            // If current node is another node in higher or same level
            for (int i = state.Level; i >= level; --i)
            {
                state.Parents.Pop();
            }

            var item = new TocItemViewModel
            {
                Name = text,
                DisplayName = displayText,
                Href = href,
                Uid = uid
            };
            if (state.Parents.Count > 0)
            {
                var parent = state.Parents.Peek();
                parent.Items ??= [];
                parent.Items.Add(item);
            }
            else
            {
                state.Root.Add(item);
            }
            state.Parents.Push(item);

            if (state.Level == level)
            {
                return state;
            }
            return new NodeState(state, level);
        }
    }

    /// <summary>
    /// 1. # [tocTitle](tocLink)
    /// 2. # [tocTitle](@uid)
    /// 3. # [tocTitle](xref:uid)
    /// </summary>
    internal sealed partial class TopicTocParseRule : ParseRule
    {
        [GeneratedRegex(@"^\s*(?:xref:|@)(\s*?\S+?[\s\S]*?)\s*$")]
        private static partial Regex UidRegex();

        [GeneratedRegex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)(\)| ""(?<displayText>.*)""\))(?:( |\t)+#*)?( |\t)*(\n|$)")]
        private static partial Regex TocRegex();

        public override Match Match(string text) => TocRegex().Match(text);

        public override ParseState Apply(ParseState state, Match match)
        {
            var tocLink = match.Groups["tocLink"].Value;
            var tocTitle = match.Groups["tocTitle"].Value;
            var headerLevel = match.Groups["headerLevel"].Value.Length;
            var uidMatch = UidRegex().Match(tocLink);
            string tocDisplayTitle = null;

            var displayGrp = match.Groups["displayText"];

            if (displayGrp.Success)
            {
                tocDisplayTitle = displayGrp.Value;
            }

            if (uidMatch.Length > 0)
            {
                return ApplyCore(state, headerLevel, tocTitle, null, uidMatch.Groups[1].Value, tocDisplayTitle);
            }

            return ApplyCore(state, headerLevel, tocTitle, tocLink, null, tocDisplayTitle);
        }
    }

    /// <summary>
    /// 1. <xref:uid>
    /// 2. <xref:"uid_containing_spaces_or_greator_than_symbol">
    /// </summary>
    internal sealed partial class TopicXrefAutoLinkTocParseRule : ParseRule
    {
        [GeneratedRegex($@"^(#+)(?: |\t)*{XrefAutoLinkRegexString}( |\t)*#*( |\t)*(\n|$)")]
        private static partial Regex XrefAutoLinkTocRegex();

        [GeneratedRegex($@"^(#+)(?: |\t)*{XrefAutoLinkRegexWithQuoteString}( |\t)*#*( |\t)*(\n|$)")]
        private static partial Regex XrefAutoLinkWithQuoteTocRegex();

        public override Match Match(string text)
        {
            var match = XrefAutoLinkWithQuoteTocRegex().Match(text);
            if (match.Length == 0)
            {
                match = XrefAutoLinkTocRegex().Match(text);
            }

            return match;
        }

        public override ParseState Apply(ParseState state, Match match)
        {
            return ApplyCore(state, match.Groups[1].Value.Length, null, null, match.Groups[3].Value);
        }
    }

    internal sealed partial class TopicXrefShortcutTocParseRule : ParseRule
    {
        [GeneratedRegex($@"^(#+)(?: |\t)*{XrefShortcutRegexString}( |\t)*#*( |\t)*(\n|$)")]
        private static partial Regex XrefShortcutTocRegex();

        [GeneratedRegex($@"^(#+)(?: |\t)*{XrefShortcutRegexWithQuoteString}( |\t)*#*( |\t)*(\n|$)")]
        private static partial Regex XrefShortcutTocWithQuoteTocRegex();

        public override Match Match(string text)
        {
            var match = XrefShortcutTocWithQuoteTocRegex().Match(text);
            if (match.Length == 0)
            {
                match = XrefShortcutTocRegex().Match(text);
            }

            return match;
        }

        public override ParseState Apply(ParseState state, Match match)
        {
            return ApplyCore(state, match.Groups[1].Value.Length, null, null, match.Groups["uid"].Value);
        }
    }

    internal sealed partial class ExternalLinkTocParseRule : ParseRule
    {
        [GeneratedRegex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+?)\]\((?<tocLink>(http[s]?://).*?)\)(?:( |\t)+#*)?( |\t)*(\n|$)")]
        private static partial Regex TocRegex();

        public override Match Match(string text) => TocRegex().Match(text);

        public override ParseState Apply(ParseState state, Match match)
        {
            return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, match.Groups["tocLink"].Value);
        }
    }

    internal sealed partial class ContainerParseRule : ParseRule
    {
        [GeneratedRegex(@"^(?<headerLevel>#+)(( |\t)*)(?<tocTitle>.+?)(?:( |\t)+#*)?( |\t)*(\n|$)")]
        private static partial Regex ContainerRegex();

        public override Match Match(string text) => ContainerRegex().Match(text);

        public override ParseState Apply(ParseState state, Match match)
        {
            return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, null);
        }
    }

    internal sealed partial class CommentParseRule : ParseRule
    {
        [GeneratedRegex(@"^\s*<!--[\s\S]*?-->\s*(\n|$)")]
        private static partial Regex CommentRegex();

        public override Match Match(string text) => CommentRegex().Match(text);

        public override ParseState Apply(ParseState state, Match match) => state;
    }

    internal sealed partial class WhitespaceParseRule : ParseRule
    {
        [GeneratedRegex(@"^\s*(\n|$)")]
        private static partial Regex WhitespaceRegex();

        public override Match Match(string text) => WhitespaceRegex().Match(text);

        public override ParseState Apply(ParseState state, Match match) => state;
    }
}
