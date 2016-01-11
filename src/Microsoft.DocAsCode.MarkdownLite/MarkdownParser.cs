// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class MarkdownParser : IMarkdownParser
    {
        private const string IndentLevelOne = "  ";
        private const string IndentLevelTwo = "    ";
        private const string ThreeDot = "...";

        public MarkdownParser(IMarkdownContext context, Options options, Dictionary<string, LinkObj> links)
        {
            Context = context;
            Options = options;
            Links = links;
        }

        public Options Options { get; }

        public IMarkdownContext Context { get; private set; }

        public Dictionary<string, LinkObj> Links { get; }

        public IMarkdownContext SwitchContext(IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return SwitchContextCore(context);
        }

        private IMarkdownContext SwitchContextCore(IMarkdownContext context)
        {
            var result = Context;
            Context = context;
            return result;
        }

        protected virtual string Preprocess(string src)
        {
            return Regexes.Lexers.WhiteSpaceLine.Replace(src, string.Empty);
        }

        public ImmutableArray<IMarkdownToken> Tokenize(string markdown)
        {
            return TokenizeCore(Preprocess(markdown)).ToImmutableArray();
        }

        private List<IMarkdownToken> TokenizeCore(string markdown)
        {
            var current = markdown;
            var tokens = new List<IMarkdownToken>();
            while (current.Length > 0)
            {
                var token = (from r in Context.Rules
                             select r.TryMatch(this, ref current)).FirstOrDefault(t => t != null);
                if (token == null)
                {
                    var nextLine = current.Split('\n')[0];
                    var lineNumber = CountNewLine(markdown, markdown.Length - current.Length);
                    throw new InvalidOperationException($"Cannot parse: {nextLine}{Environment.NewLine}{GetMarkdownContext(markdown, lineNumber)}{GetRuleContextMessage()}.");
                }
                tokens.Add(token);
            }
            return tokens;
        }

        private static string GetMarkdownContext(string markdown, int lineNumber)
        {
            var lines = markdown.Split('\n');
            StringBuffer sb = IndentLevelOne;
            sb += "markdown context:";
            if (lineNumber > 0)
            {
                if (lineNumber > 1)
                {
                    sb += IndentLevelTwo;
                    sb += ThreeDot;
                    sb += Environment.NewLine;
                }
                sb += IndentLevelTwo;
                sb += lines[lineNumber - 1];
                sb += Environment.NewLine;
            }
            sb += IndentLevelTwo;
            sb += lines[lineNumber];
            sb += Environment.NewLine;
            if (lineNumber + 1 < lines.Length)
            {
                sb += IndentLevelTwo;
                sb += lines[lineNumber + 1];
                sb += Environment.NewLine;
                if (lineNumber + 2 < lines.Length)
                {
                    sb += IndentLevelTwo;
                    sb += ThreeDot;
                    sb += Environment.NewLine;
                }
            }
            return sb.ToString();
        }

        private string GetRuleContextMessage()
        {
            StringBuffer sb = IndentLevelOne;
            sb += "rule context: ";
            sb += Context.GetType().Name;
            if (Context.Variables?.Count > 0)
            {
                sb += Environment.NewLine;
                sb += IndentLevelOne;
                sb += "{";
                foreach (var item in Context.Variables)
                {
                    sb += Environment.NewLine;
                    sb += IndentLevelTwo;
                    sb += item.Key;
                    sb += " = ";
                    if (item.Value == null)
                    {
                        sb += "(null)";
                    }
                    else
                    {
                        sb += item.Value.ToString();
                    }
                    sb += ";";
                }
                sb += Environment.NewLine;
                sb += IndentLevelOne;
                sb += "}";
            }
            return sb.ToString();
        }

        internal static int CountNewLine(string text, int charCount)
        {
            int count = 0;
            for (int i = 0; i < charCount; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }
            return count;
        }
    }
}
