namespace Microsoft.DocAsCode.MarkdownLite
{
    using CSharp.RuntimeBinder;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class MarkdownEngine : ICloneable
    {
        public MarkdownEngine(IMarkdownContext context, object renderer, Options options)
            : this(context, renderer, options, new Dictionary<string, LinkObj>())
        {
        }

        protected MarkdownEngine(IMarkdownContext context, object renderer, Options options, Dictionary<string, LinkObj> links)
        {
            Context = context;
            Renderer = renderer;
            Options = options;
            Links = links;
        }

        public object Renderer { get; }

        public Options Options { get; }

        public IMarkdownContext Context { get; }

        public Dictionary<string, LinkObj> Links { get; }

        public StringBuffer Render(IMarkdownToken token, IMarkdownContext context)
        {
            try
            {
                // double dispatch.
                return ((dynamic)Renderer).Render(this, (dynamic)token, (dynamic)context);
            }
            catch (RuntimeBinderException ex)
            {
                throw new InvalidOperationException($"Unable to handle token: {token.GetType().Name}, rule: {token.Rule.Name}", ex);
            }
        }

        public virtual MarkdownEngine ChangeContext(IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context == Context)
            {
                return this;
            }
            return new MarkdownEngine(context, Renderer, Options, Links);
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
                result += Render(token, Context);
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

        private List<IMarkdownToken> TokenizeCore(string markdown)
        {
            var current = markdown;
            var tokens = new List<IMarkdownToken>();
            while (current.Length > 0)
            {
                var token = (from r in Context.GetRules()
                             select r.TryMatch(this, ref current)).FirstOrDefault(t => t != null);
                if (token == null)
                {
                    throw new InvalidOperationException($"Cannot parse: {current.Split('\n')[0]}.");
                }
                tokens.Add(token);
            }
            return tokens;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
