// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class Marked
    {
        protected Lexer Lexer { get; private set; }
        protected Parser Parser { get; private set; }

        public Options Options { get; private set; }

        public Marked(Options options = null, Lexer lexer = null, Parser parser = null)
        {
            options = options ?? new Options();
            Options = options;
            Lexer = lexer ?? new Lexer(options);
            Parser = parser ?? new Parser(options);
        }

        public virtual string Parse(string src)
        {
            var tokens = Lexer.Lex(src);
            return Parser.Parse(tokens);
        }

        public static string Markup(string source, Options options = null)
        {
            var marked = new Marked(options);
            return marked.Parse(source);
        }
    }
}
