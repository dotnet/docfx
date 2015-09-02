namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public static class MarkdownTocReader
    {
        public static TocViewModel LoadToc(string tocContent, string filePath)
        {
            ParseState state = new InitialState(filePath);
            var rules = new ParseRule[]
            {
                new TopicTocParseRule(),
                new ExternalLinkTocParseRule(),
                new ContainerParseRule(),
                new CommentParseRule(),
                new WhitespaceParseRule(),
            };
            var content = tocContent;
            while (content.Length > 0)
            {
                state = state.ApplyRules(rules, ref content);
            }
            return state.Root;
        }

        internal abstract class ParseState
        {
            public abstract int Level { get; }
            public abstract Stack<TocItemViewModel> Parents { get; }
            public abstract TocViewModel Root { get; }
            public abstract string FilePath { get; }
            public virtual ParseState ApplyRules(ParseRule[] rules, ref string input)
            {
                foreach (var rule in rules)
                {
                    var m = rule.Match(input);
                    if (m.Success)
                    {
                        input = input.Substring(m.Length);
                        return rule.Apply(this, m);
                    }
                }
                var message = input.Length <= 20 ? input : input.Remove(20) + "...";
                return new ErrorState(this, Level, $"Unknown syntax: {message}");
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
            public override ParseState ApplyRules(ParseRule[] rules, ref string input)
            {
                throw new InvalidDataException($"Invalid toc file: {FilePath}, Details: {Message}");
            }
        }

        internal abstract class ParseRule
        {
            public abstract Match Match(string text);

            public abstract ParseState Apply(ParseState state, Match match);

            protected ParseState ApplyCore(ParseState state, int level, string text, string href)
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
                    Href = href,
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

        internal sealed class TopicTocParseRule : ParseRule
        {
            public static readonly Regex TocRegex =
                new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\r?\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => TocRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match)
            {
                return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, match.Groups["tocLink"].Value);
            }
        }

        internal sealed class ExternalLinkTocParseRule : ParseRule
        {
            public static readonly Regex TocRegex =
                new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+?)\]\((?<tocLink>(http[s]?://).*?)\)( |\t)*#*( |\t)*(\r?\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => TocRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match)
            {
                return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, match.Groups["tocLink"].Value);
            }
        }

        internal sealed class ContainerParseRule : ParseRule
        {
            public static readonly Regex ContainerRegex =
                new Regex(@"^(?<headerLevel>#+)(( |\t)*)(?<tocTitle>.+?)( |\t)*#*( |\t)*(\r?\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => ContainerRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match)
            {
                return ApplyCore(state, match.Groups["headerLevel"].Value.Length, match.Groups["tocTitle"].Value, null);
            }
        }

        internal sealed class CommentParseRule : ParseRule
        {
            public static readonly Regex CommentRegex =
                new Regex(@"^\s*<!--[\s\S]*?-->\s*(\r?\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => CommentRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match) => state;
        }

        internal sealed class WhitespaceParseRule : ParseRule
        {
            public static readonly Regex WhitespaceRegex =
                new Regex(@"^\s*(\r?\n|$)", RegexOptions.Compiled);

            public override Match Match(string text) => WhitespaceRegex.Match(text);

            public override ParseState Apply(ParseState state, Match match) => state;
        }
    }
}
