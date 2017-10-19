namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Plugins;

    public static class MarkdownTocReader
    {
        public static TocViewModel LoadToc(string tocContent, string filePath)
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
            public abstract TocViewModel Root { get; }
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
                Root = new TocViewModel();
                FilePath = filePath;
            }
            public override int Level => 0;
            public override Stack<TocItemViewModel> Parents { get; }
            public override string FilePath { get; }
            public override TocViewModel Root { get; }
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
            public override TocViewModel Root { get; }
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
            public override TocViewModel Root { get; }
            public override string FilePath { get; }
            public override ParseState ApplyRules(ParseRule[] rules, ref string input, ref int lineNumber)
            {
                throw new DocumentException($"Invalid toc file: {FilePath}, Details: {Message}");
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
                    if (parent.Items == null)
                    {
                        parent.Items = new TocViewModel();
                    }
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
        internal sealed class TopicTocParseRule : ParseRule
        {
            private static readonly Regex UidRegex = new Regex(@"^\s*(?:xref:|@)(\s*?\S+?[\s\S]*?)\s*$", RegexOptions.Compiled);
            public static readonly Regex TocRegex =
                new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)(\)| ""(?<displayText>.*)""\))(?:( |\t)+#*)?( |\t)*(\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => TocRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match)
            {
                var tocLink = match.Groups["tocLink"].Value;
                var tocTitle = match.Groups["tocTitle"].Value;
                var headerLevel = match.Groups["headerLevel"].Value.Length;
                var uidMatch = UidRegex.Match(tocLink);
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
        internal sealed class TopicXrefAutoLinkTocParseRule : ParseRule
        {
            public static readonly Regex XrefAutoLinkTocRegex =
                new Regex($@"^(#+)(?: |\t)*{DfmXrefAutoLinkInlineRule.XrefAutoLinkRegexString}( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);
            public static readonly Regex XrefAutoLinkWithQuoteTocRegex =
                new Regex($@"^(#+)(?: |\t)*{DfmXrefAutoLinkInlineRule.XrefAutoLinkRegexWithQuoteString}( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

            public override Match Match(string text)
            {
                var match = XrefAutoLinkWithQuoteTocRegex.Match(text);
                if (match.Length == 0)
                {
                    match = XrefAutoLinkTocRegex.Match(text);
                }

                return match;
            }

            public override ParseState Apply(ParseState state, Match match)
            {
                return ApplyCore(state, match.Groups[1].Value.Length, null, null, match.Groups[3].Value);
            }
        }

        internal sealed class TopicXrefShortcutTocParseRule : ParseRule
        {
            public static readonly Regex XrefShortcutTocRegex =
                new Regex($@"^(#+)(?: |\t)*{DfmXrefShortcutInlineRule.XrefShortcutRegexString}( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);
            public static readonly Regex XrefShortcutTocWithQuoteTocRegex =
                new Regex($@"^(#+)(?: |\t)*{DfmXrefShortcutInlineRule.XrefShortcutRegexWithQuoteString}( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

            public override Match Match(string text)
            {
                var match = XrefShortcutTocWithQuoteTocRegex.Match(text);
                if (match.Length == 0)
                {
                    match = XrefShortcutTocRegex.Match(text);
                }

                return match;
            }

            public override ParseState Apply(ParseState state, Match match)
            {
                return ApplyCore(state, match.Groups[1].Value.Length, null, null, match.Groups["uid"].Value);
            }
        }

        internal sealed class ExternalLinkTocParseRule : ParseRule
        {
            public static readonly Regex TocRegex =
                new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+?)\]\((?<tocLink>(http[s]?://).*?)\)(?:( |\t)+#*)?( |\t)*(\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => TocRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match)
            {
                return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, match.Groups["tocLink"].Value);
            }
        }

        internal sealed class ContainerParseRule : ParseRule
        {
            public static readonly Regex ContainerRegex =
                new Regex(@"^(?<headerLevel>#+)(( |\t)*)(?<tocTitle>.+?)(?:( |\t)+#*)?( |\t)*(\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => ContainerRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match)
            {
                return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, null);
            }
        }

        internal sealed class CommentParseRule : ParseRule
        {
            public static readonly Regex CommentRegex =
                new Regex(@"^\s*<!--[\s\S]*?-->\s*(\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => CommentRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match) => state;
        }

        internal sealed class WhitespaceParseRule : ParseRule
        {
            public static readonly Regex WhitespaceRegex =
                new Regex(@"^\s*(\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => WhitespaceRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match) => state;
        }
    }
}
