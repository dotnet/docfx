// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.IO;

    using MarkdownLite;

    using Utility;

    public class DfmRenderer : MarkdownRenderer
    {
        private static readonly DocfxFlavoredIncHelper _inlineInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DocfxFlavoredIncHelper _blockInclusionHelper = new DocfxFlavoredIncHelper();
        public virtual StringBuffer Render(MarkdownEngine engine, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            var href = token.Href == null ? string.Empty : $" href=\"{StringHelper.HtmlEncode(token.Href)}\"";
            var name = token.Name == null ? null : StringHelper.HtmlEncode(token.Name);
            var title = token.Title == null ? null : $" title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            return $"<xref{href}{title}>{name}</xref>";
        }

        public virtual StringBuffer Render(DfmEngine engine, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            var href = token.Src == null ? null : $"src=\"{StringHelper.HtmlEncode(token.Src)}\"";
            var name = token.Name == null ? null : StringHelper.HtmlEncode(token.Name);
            var title = token.Title == null ? null : $"title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            var resolved = _blockInclusionHelper.Load(token.Src, token.Raw, engine.Parents, engine.InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(DfmEngine engine, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            var resolved = _inlineInclusionHelper.Load(token.Src, token.Raw, engine.Parents, engine.InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(MarkdownEngine engine, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            var content = token.Content == null ? string.Empty : StringHelper.HtmlEncode(token.Content);
            return $"<yamlheader>{content}</yamlheader>";
        }

        public override StringBuffer Render(MarkdownEngine engine, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
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

        public virtual StringBuffer Render(MarkdownEngine engine, DfmSectionBeginBlockToken token, MarkdownBlockContext context)
        {
            return $"<div{token.Attributes}>";
        }

        public virtual StringBuffer Render(MarkdownEngine engine, DfmSectionEndBlockToken token, MarkdownBlockContext context)
        {
            return $"</div>";
        }

        public virtual StringBuffer Render(DfmEngine engine, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            var lang = string.IsNullOrEmpty(token.Lang) ? null : $" class=\"language-{token.Lang}\"";
            var name = string.IsNullOrEmpty(token.Name) ? null : $" name=\"{StringHelper.HtmlEncode(token.Name)}\"";
            var title = string.IsNullOrEmpty(token.Title) ? null : $" title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            if (!PathUtility.IsRelativePath(token.Path))
            {
                string errorMessage = $"Code absolute path: {token.Path} is not supported in file {engine.Parents.Peek()}";
                Logger.LogError(errorMessage);
                return $"<!-- {StringHelper.HtmlEncode(errorMessage)} -->";
            }

            try
            {
                // TODO: Valid REST and REST-i script.
                var fencesPath = ((RelativePath)token.Path).BasedOn((RelativePath)engine.Parents.Peek());
                var fencesCode = File.ReadAllText(fencesPath);
                return $"<pre><code{lang}{name}{title}>{StringHelper.HtmlEncode(fencesCode)}\n</code></pre>";
            }
            catch(FileNotFoundException)
            {
                string errorMessage = $"Can not find reference {token.Path}";
                Logger.LogError(errorMessage);
                return $"<!-- {StringHelper.HtmlEncode(errorMessage)} -->";
            }
        }
    }

    public class DfmYamlHeaderBlockToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public string Content { get; }
        public string RawMarkdown { get; set; }
        public DfmYamlHeaderBlockToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }
    }

    public class DfmXrefInlineToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public string Href { get; }
        public string Name { get; }
        public string Title { get; }
        public string RawMarkdown { get; set; }

        public DfmXrefInlineToken(IMarkdownRule rule, string href, string name, string title)
        {
            Rule = rule;
            Href = href;
            Name = name;
            Title = title;
        }
    }

    public class DfmIncludeBlockToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public string Src { get; }
        public string Name { get; }
        public string Title { get; }
        public string Raw { get; }
        public string RawMarkdown { get; set; }

        public DfmIncludeBlockToken(IMarkdownRule rule, string src, string name, string title, string raw)
        {
            Rule = rule;
            Src = src;
            Name = name;
            Title = title;
            Raw = raw;
        }
    }

    public class DfmIncludeInlineToken: IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public string Src { get; }
        public string Name { get; }
        public string Title { get; }
        public string Raw { get; }
        public string RawMarkdown { get; set; }

        public DfmIncludeInlineToken(IMarkdownRule rule, string src, string name, string title, string raw)
        {
            Rule = rule;
            Src = src;
            Name = name;
            Title = title;
            Raw = raw;
        }
    }
}
