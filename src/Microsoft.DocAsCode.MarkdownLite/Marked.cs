// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public static class Marked
    {
        public static string Markup(string source, Options options = null)
        {
            options = options ?? new Options();
            var lexer = new Lexer(options);
            var tokens = lexer.Lex(source);
            return new Parser(tokens, options).Parse();
        }
    }
}
