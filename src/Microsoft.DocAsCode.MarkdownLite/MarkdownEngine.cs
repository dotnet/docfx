﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using CSharp.RuntimeBinder;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class MarkdownEngine : ICloneable
    {
        private const string IndentLevelOne = "  ";
        private const string IndentLevelTwo = "    ";
        private const string ThreeDot = "...";

        public MarkdownEngine(IMarkdownContext context, object renderer, Options options)
            : this(context, null, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        public MarkdownEngine(IMarkdownContext context, IMarkdownRewriter rewriter, object renderer, Options options)
            : this(context, rewriter, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        protected MarkdownEngine(IMarkdownContext context, IMarkdownRewriter rewriter, object renderer, Options options, Dictionary<string, LinkObj> links)
        {
            Context = context;
            Rewriter = rewriter ?? MarkdownRewriterFactory.Null;
            Renderer = renderer;
            Options = options;
            Links = links;
        }

        public object Renderer { get; }

        public Options Options { get; }

        public IMarkdownContext Context { get; private set; }

        public IMarkdownRewriter Rewriter { get; }

        public Dictionary<string, LinkObj> Links { get; }

        public StringBuffer Render(IMarkdownToken token)
        {
            var rewrittenToken = RewriteToken(token);
            try
            {
                // double dispatch.
                return ((dynamic)Renderer).Render((dynamic)this, (dynamic)rewrittenToken, (dynamic)rewrittenToken.Context);
            }
            catch (RuntimeBinderException ex)
            {
                throw new InvalidOperationException($"Unable to handle token: {rewrittenToken.GetType().Name}, rule: {token.Rule.Name}", ex);
            }
        }

        private IMarkdownToken RewriteToken(IMarkdownToken token)
        {
            var rewritedToken = token;
            var newToken = Rewriter.Rewrite(this, rewritedToken);
            if (newToken != null)
            {
                rewritedToken = newToken;
            }
            return rewritedToken;
        }

        public IMarkdownContext SwitchContext(string variableKey, object value)
        {
            if (variableKey == null)
            {
                throw new ArgumentNullException(nameof(variableKey));
            }
            return SwitchContextCore(
                Context.CreateContext(
                    Context.Variables.SetItem(variableKey, value)));
        }

        public IMarkdownContext SwitchContext(IReadOnlyDictionary<string, object> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }
            var builder = Context.Variables.ToBuilder();
            foreach (var pair in variables)
            {
                builder[pair.Key] = pair.Value;
            }
            return SwitchContextCore(
                Context.CreateContext(
                    builder.ToImmutable()));
        }

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

        public string Normalize(string markdown)
        {
            return markdown
                .ReplaceRegex(Regexes.Lexers.NormalizeNewLine, "\n")
                .Replace("\t", "    ")
                .Replace("\u00a0", " ")
                .Replace("\u2424", "\n");
        }

        public StringBuffer Mark(string markdown)
        {
            var result = StringBuffer.Empty;
            foreach (var token in TokenizeCore(Preprocess(markdown)))
            {
                result += Render(token);
            }
            return result;
        }

        public string Markup(string markdown)
        {
            return Mark(Normalize(markdown)).ToString();
        }

        protected virtual string Preprocess(string src)
        {
            return Regexes.Lexers.WhiteSpaceLine.Replace(src, string.Empty);
        }

        public ImmutableArray<IMarkdownToken> Tokenize(string markdown)
        {
            return TokenizeCore(Preprocess(markdown)).ToImmutableArray();
        }

        public ImmutableArray<IMarkdownToken> TokenizeInline(string markdown)
        {
            var context = Context as MarkdownBlockContext;
            if (markdown == null)
            {
                throw new InvalidOperationException($"{nameof(Context)}(type:{Context.GetType().FullName}) is invalid.");
            }
            var c = SwitchContext(context.InlineContext);
            var tokens = Tokenize(markdown);
            SwitchContext(c);
            return tokens;
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
                    var lineNumber = markdown.Take(markdown.Length - current.Length).Count(c => c == '\n');
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

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
