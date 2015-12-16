// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.IO;
    using MarkdownLite;

    using Utility;

    public class DfmRenderer : MarkdownRenderer
    {
        private static readonly DocfxFlavoredIncHelper _inlineInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DocfxFlavoredIncHelper _blockInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DfmCodeExtractor _dfmCodeExtractor = new DfmCodeExtractor();

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            var href = token.Href == null ? string.Empty : $" href=\"{StringHelper.HtmlEncode(token.Href)}\"";
            var name = token.Name == null ? null : StringHelper.HtmlEncode(token.Name);
            var title = token.Title == null ? null : $" title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            return $"<xref{href}{title}>{name}</xref>";
        }

        public virtual StringBuffer Render(DfmRendererAdapter engine, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            var href = token.Src == null ? null : $"src=\"{StringHelper.HtmlEncode(token.Src)}\"";
            var name = token.Name == null ? null : StringHelper.HtmlEncode(token.Name);
            var title = token.Title == null ? null : $"title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            var resolved = _blockInclusionHelper.Load(engine, token.Src, token.Raw, context, engine.Engine.InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(DfmRendererAdapter engine, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            var resolved = _inlineInclusionHelper.Load(engine, token.Src, token.Raw, context, engine.Engine.InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            var content = token.Content == null ? string.Empty : StringHelper.HtmlEncode(token.Content);
            return $"<yamlheader>{content}</yamlheader>";
        }

        public override StringBuffer Render(IMarkdownRenderer engine, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            if (token.Tokens.Length > 0)
            {
                var ft = token.Tokens[0] as DfmNoteBlockToken;
                if (ft != null)
                {
                    return $"<blockquote class=\"{ft.NoteType}\">" + RenderTokens(engine, token.Tokens.RemoveAt(0), context, true, token.Rule) + "</blockquote>";
                }
            }

            return base.Render(engine, token, context);
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmSectionBeginBlockToken token, MarkdownBlockContext context)
        {
            return $"<div{token.Attributes}>";
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmSectionEndBlockToken token, MarkdownBlockContext context)
        {
            return $"</div>";
        }

        public virtual StringBuffer Render(DfmRendererAdapter engine, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            if (!PathUtility.IsRelativePath(token.Path))
            {
                string errorMessage = $"Code absolute path: {token.Path} is not supported in file {engine.GetFilePathStack(context).Peek()}";
                Logger.LogError(errorMessage);
                return GetRenderedFencesBlockString(token, errorMessage);
            }

            try
            {
                // TODO: Valid REST and REST-i script.
                var fencesPath = ((RelativePath)token.Path).BasedOn((RelativePath)engine.GetFilePathStack(context).Peek());
                var extractResult = _dfmCodeExtractor.ExtractFencesCode(token, fencesPath);
                return GetRenderedFencesBlockString(token, extractResult.ErrorMessage, extractResult.FencesCodeLines);
            }
            catch (FileNotFoundException)
            {
                string errorMessage = $"Can not find reference {token.Path}";
                Logger.LogError(errorMessage);
                return GetRenderedFencesBlockString(token, errorMessage);
            }
        }

        private static string GetRenderedFencesBlockString(DfmFencesBlockToken token, string errorMessage, string[] codeLines = null)
        {
            string renderedErrorMessage = string.Empty;
            string renderedCodeLines = string.Empty;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                renderedErrorMessage = $@"<!-- {StringHelper.HtmlEncode(errorMessage)} -->\n";
            }

            if (codeLines != null)
            {
                var lang = string.IsNullOrEmpty(token.Lang) ? null : $" class=\"language-{token.Lang}\"";
                var name = string.IsNullOrEmpty(token.Name) ? null : $" name=\"{StringHelper.HtmlEncode(token.Name)}\"";
                var title = string.IsNullOrEmpty(token.Title) ? null : $" title=\"{StringHelper.HtmlEncode(token.Title)}\"";

                renderedCodeLines = $"<pre><code{lang}{name}{title}>{StringHelper.HtmlEncode(string.Join("\n", codeLines))}\n</code></pre>";
            }

            return $"{renderedErrorMessage}{renderedCodeLines}";
        }
    }

    public class DfmYamlHeaderBlockToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Content { get; }
        public string RawMarkdown { get; set; }

        public DfmYamlHeaderBlockToken(IMarkdownRule rule, IMarkdownContext context, string content)
        {
            Rule = rule;
            Context = context;
            Content = content;
        }
    }

    public class DfmXrefInlineToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Href { get; }
        public string Name { get; }
        public string Title { get; }
        public string RawMarkdown { get; set; }

        public DfmXrefInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, string name, string title)
        {
            Rule = rule;
            Context = context;
            Href = href;
            Name = name;
            Title = title;
        }
    }

    public class DfmIncludeBlockToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Src { get; }
        public string Name { get; }
        public string Title { get; }
        public string Raw { get; }
        public string RawMarkdown { get; set; }

        public DfmIncludeBlockToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, string raw)
        {
            Rule = rule;
            Context = context;
            Src = src;
            Name = name;
            Title = title;
            Raw = raw;
        }
    }

    public class DfmIncludeInlineToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Src { get; }
        public string Name { get; }
        public string Title { get; }
        public string Raw { get; }
        public string RawMarkdown { get; set; }

        public DfmIncludeInlineToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, string raw)
        {
            Rule = rule;
            Context = context;
            Src = src;
            Name = name;
            Title = title;
            Raw = raw;
        }
    }
}
