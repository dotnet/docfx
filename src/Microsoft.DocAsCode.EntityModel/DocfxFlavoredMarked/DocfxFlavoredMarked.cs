// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{

    using MarkdownLite;
    using System.Collections.Generic;

    public class DocfxFlavoredMarked : Marked
    {
        public static DocfxFlavoredMarked Default = new DocfxFlavoredMarked();

        private readonly DocfxFlavoredIncHelper _incHelper = new DocfxFlavoredIncHelper();

        public DocfxFlavoredMarked() : this(new DocfxFlavoredOptions())
        {
        }

        public DocfxFlavoredMarked(DocfxFlavoredOptions options) : base(options, new DocfxFlavoredLexer(options), new DocfxFlavoredParser(options))
        {
        }

        // todo : temp use gfm.
        private static readonly MarkdownEngine _engine = new GfmEngineBuilder(new Options()).CreateEngine(new MarkdownRenderer());

        public static string Markup(string src, string path = null, DocfxFlavoredOptions options = null)
        {
            return _engine.Markup(src);
            //var marked = options == null ? Default : new DocfxFlavoredMarked(options);

            //return marked.Parse(src, path);
        }

        public string Parse(string src, string path)
        {
            if (string.IsNullOrEmpty(src) && string.IsNullOrEmpty(path)) return string.Empty;
            return _incHelper.Load(path, string.Empty, string.Empty, string.Empty, null, src, InternalParse, MarkdownNodeType.Block, (DocfxFlavoredOptions)Options);
        }

        private string InternalParse(string src, Stack<string> parents)
        {
            var tokens = Lexer.Lex(src);
            return ((DocfxFlavoredParser)Parser).Parse(tokens, parents);
        }
    }
}
