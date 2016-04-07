// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    internal static class Regexes
    {
        private const RegexOptions RegexOptionCompiled =
#if NetCore
            RegexOptions.None;
#else
            RegexOptions.Compiled;
#endif

        public static class Block
        {
            public static readonly Regex Newline = new Regex(@"^\n+", RegexOptionCompiled);
            public static readonly Regex Code = new Regex(@"^( {4}[^\n]+\n*)+", RegexOptionCompiled);
            public static readonly Regex Hr = new Regex(@"^( *[-*_]){3,} *(?:\n+|$)", RegexOptionCompiled);
            public static readonly Regex Heading = new Regex(@"^ *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)", RegexOptionCompiled);
            public static readonly Regex LHeading = new Regex(@"^([^\n]+)\n *(=|-){2,} *(?:\n+|$)", RegexOptionCompiled);
            public static readonly Regex Blockquote = new Regex(@"^( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+", RegexOptionCompiled);
            public static readonly Regex List = new Regex(@"^( *)((?:[*+-]|\d+\.)) [\s\S]+?(?:\n+(?=([^\n]+)\n(=|-){2,} *(?:\n+|$))|\n+(?=\1?(?:[-*_] *){3,}(?:\n+|$))|\n+(?=\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! )(?!\s*\1(?:[*+-]|\d+\.) )\n*|\s*$)", RegexOptionCompiled);
            public static readonly Regex Html = new Regex(@"^ *(?:<!--[\s\S]*?-->|<((?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b)[\s\S]+?<\/\1>|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b(?:""[^""]*""|'[^']*'|[^'"">])*?>) *(?:\n{2,}|\s*$)", RegexOptionCompiled);
            public static readonly Regex Def = new Regex(@"^ *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)", RegexOptionCompiled);
            public static readonly Regex Paragraph = new Regex(@"^((?:[^\n]+\n?(?!( *[-*_]){3,} *(?:\n+|$)| *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)|([^\n]+)\n *(=|-){2,} *(?:\n+|$)|( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b| *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)))+)\n*", RegexOptionCompiled);
            public static readonly Regex Text = new Regex(@"^[^\n]+", RegexOptionCompiled);
            public static readonly Regex Bullet = new Regex(@"(?:[*+-]|\d+\.)", RegexOptionCompiled);
            public static readonly Regex Item = new Regex(@"^( *)((?:[*+-]|\d+\.)) [^\n]*(?:\n(?!\1(?:[*+-]|\d+\.) )[^\n]*)*", RegexOptions.Multiline | RegexOptionCompiled);

            public static class Gfm
            {
                public static readonly Regex Fences = new Regex(@"^ *(`{3,}|~{3,}) *(\S+)? *\n([\s\S]+?)\s*\1 *(?:\n+|$)", RegexOptionCompiled);
                public static readonly Regex Paragraph = new Regex(@"^((?:[^\n]+\n?(?! *(`{3,}|~{3,}) *(\S+)? *\n([\s\S]+?)\s*\2 *(?:\n+|$)|( *)((?:[*+-]|\d+\.)) [\s\S]+?(?:\n+(?=\5?(?:[-*_] *){3,}(?:\n+|$))|\n+(?= *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! )(?!\5(?:[*+-]|\d+\.) )\n*|\s*$)|( *[-*_]){3,} *(?:\n+|$)| *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)|([^\n]+)\n *(=|-){2,} *(?:\n+|$)|( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b| *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)))+)\n*", RegexOptionCompiled);
                public static readonly Regex Heading = new Regex(@"^ *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)", RegexOptionCompiled);
            }

            public static class Tables
            {
                public static readonly Regex NpTable = new Regex(@"^ *(\S.*\|.*)\n *([-:]+ *\|[-| :]*)\n((?:.*\|.*(?:\n|$))*)\n*", RegexOptionCompiled);
                public static readonly Regex Table = new Regex(@"^ *\|(.+)\n *\|( *[-:]+[-| :]*)\n((?: *\|.*(?:\n|$))*)\n*", RegexOptionCompiled);
            }
        }

        public static class Inline
        {
            public static readonly Regex Escape = new Regex(@"^\\([\\`*{}\[\]()#+\-.!_>])", RegexOptionCompiled);
            public static readonly Regex Comment = new Regex(@"^<!--[\s\S]*?-->", RegexOptionCompiled);
            public static readonly Regex AutoLink = new Regex(@"^<([^ >]+(@|:\/)[^ >]+)>", RegexOptionCompiled);
            public static readonly Regex CodeElement = new Regex(@"^\<code\>[\s\S]*?\</code\>", RegexOptionCompiled | RegexOptions.IgnoreCase);
            public static readonly Regex Tag = new Regex(@"^<\/?\w+(?:""[^""]*""|'[^']*'|[^'"">])*?>", RegexOptionCompiled);
            /// <summary>
            /// <![CDATA[
            /// ^                                           start of string
            /// !?                                          '!' 0~1
            /// \[                                          '['
            /// ((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)    group 1: text
            /// \]                                          ']'
            /// \s*                                         white spaces
            /// \(                                          '('
            /// \s*                                         white spaces
            /// <?                                          '<' 0~1
            /// (                                           start group 2: link
            ///     (?:                                     start non-capturing group
            ///         [^()]                               any chararacter but '(' or ')'
            ///         |                                   or
            ///         \((?<DEPTH>)                        '(' with depth++
            ///         |                                   or
            ///         \)(?<-DEPTH>)                       ')' with depth--
            ///     )                                       end non-capturing group
            ///     +?                                      lazy 1~
            ///     (?(DEPTH)(?!))                          require depth = 0
            /// )                                           end group 2: link
            /// >?                                          '>' 0~1
            /// (?:                                         start non-capturing group
            ///     \s+                                     white spaces
            ///     (['""])                                 group 3: quotes
            ///     ([\s\S]*?)                              group 4: title
            ///     \3                                      ref group 3
            /// )?                                          end non-capturing group 0~1
            /// \s*                                         white spaces
            /// \)                                          ')'
            /// ]]>
            /// </summary>
            public static readonly Regex Link = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\s*\(\s*<?((?:[^()]|\((?<DEPTH>)|\)(?<-DEPTH>))+?(?(DEPTH)(?!)))>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)", RegexOptionCompiled);
            public static readonly Regex RefLink = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\s*\[([^\]]*)\]", RegexOptionCompiled);
            public static readonly Regex NoLink = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]])*)\]", RegexOptionCompiled);
            public static readonly Regex Strong = new Regex(@"^__([\s\S]+?)__(?!_)|^\*\*([\s\S]+?)\*\*(?!\*)", RegexOptionCompiled);
            public static readonly Regex Em = new Regex(@"^\b_((?:__|[\s\S])+?)_\b|^\*((?:\*\*|[\s\S])+?)\*(?!\*)", RegexOptionCompiled);
            public static readonly Regex Code = new Regex(@"^(`+)\s*([\s\S]*?[^`])\s*\1(?!`)", RegexOptionCompiled);
            public static readonly Regex Br = new Regex(@"^ {2,}\n(?!\s*$)", RegexOptionCompiled);
            public static readonly Regex EscapedText = new Regex(@"^\\([`~!#^&*_=+?.<>(){}\-\\\[\]])", RegexOptionCompiled);
            public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`]| {2,}\n|$)", RegexOptionCompiled);

            public static class Pedantic
            {
                public static readonly Regex Strong = new Regex(@"^__(?=\S)([\s\S]*?\S)__(?!_)|^\*\*(?=\S)([\s\S]*?\S)\*\*(?!\*)", RegexOptionCompiled);
                public static readonly Regex Em = new Regex(@"^_(?=\S)([\s\S]*?\S)_(?!_)|^\*(?=\S)([\s\S]*?\S)\*(?!\*)", RegexOptionCompiled);
            }

            public static class Gfm
            {
                public static readonly Regex Escape = new Regex(@"^\\([\\`*{}\[\]()#+\-.!_>~|])", RegexOptionCompiled);
                public static readonly Regex Url = new Regex(@"^(https?:\/\/[^\s<]+[^<.,:;""')\]\s])", RegexOptionCompiled);
                public static readonly Regex Del = new Regex(@"^~~(?=\S)([\s\S]*?\S)~~", RegexOptionCompiled);
                public static readonly Regex StrongEm = new Regex(@"^(\**?)\*{3}(?!\*)(?=\S)([\s\S]*?\S)\*([\s\S]*?(?<=\S))?\*{2}", RegexOptionCompiled);
                public static readonly Regex Strong = new Regex(@"^__([\s\S]+?)__(?!_)|^\*{2}(?!\*)(?=\S)([\s\S]*?\S)?\*{2}", RegexOptionCompiled);
                public static readonly Regex Em = new Regex(@"^\b_((?:__|[\s\S])+?)_\b|^\*((?:\*\*|\S[\s\S]*?))(?<!\*)\*", RegexOptionCompiled);
                public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`~]|https?:\/\/| {2,}\n|$)", RegexOptionCompiled);
            }

            public static class Breaks
            {
                public static readonly Regex Br = new Regex(@"^ *\n(?!\s*$)", RegexOptionCompiled);
                public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`~]|https?:\/\/| *\n|$)", RegexOptionCompiled);
            }

            public static class Smartypants
            {
                public static readonly Regex OpeningSingles = new Regex(@"(^|[-\u2014/(\[{""\s])'", RegexOptionCompiled);
                public static readonly Regex OpeningDoubles = new Regex(@"(^|[-\u2014/(\[{\u2018\s])""", RegexOptionCompiled);
            }
        }

        public static class Lexers
        {
            public static readonly Regex NormalizeNewLine = new Regex(@"\r\n|\r", RegexOptionCompiled);
            public static readonly Regex WhiteSpaceLine = new Regex(@"^ +$", RegexOptions.Multiline | RegexOptionCompiled);
            public static readonly Regex WhiteSpaces = new Regex(@"\s+", RegexOptionCompiled);

            public static readonly Regex LeadingWhiteSpaces = new Regex(@"^ {4}", RegexOptions.Multiline | RegexOptionCompiled);
            public static readonly Regex TailingEmptyLines = new Regex(@"\n+$", RegexOptionCompiled);

            public static readonly Regex UselessTableHeader = new Regex(@"^ *| *\| *$", RegexOptionCompiled);
            public static readonly Regex UselessTableAlign = new Regex(@"^ *|\| *$", RegexOptionCompiled);
            public static readonly Regex UselessGfmTableCell = new Regex(@"(?: *\| *)?\n$", RegexOptionCompiled);
            public static readonly Regex EmptyGfmTableCell = new Regex(@"^ *\| *| *\| *$", RegexOptionCompiled);
            public static readonly Regex TableSplitter = new Regex(@" *\| *", RegexOptionCompiled);
            public static readonly Regex EndWithNewLine = new Regex(@"\n$", RegexOptionCompiled);
            public static readonly Regex TableAlignRight = new Regex(@"^ *-+: *$", RegexOptionCompiled);
            public static readonly Regex TableAlignCenter = new Regex(@"^ *:-+: *$", RegexOptionCompiled);
            public static readonly Regex TableAlignLeft = new Regex(@"^ *:-+ *$", RegexOptionCompiled);

            public static readonly Regex LeadingBlockquote = new Regex(@"^ *> ?", RegexOptions.Multiline | RegexOptionCompiled);
            public static readonly Regex LeadingBullet = new Regex(@"^ *([*+-]|\d+\.) +", RegexOptionCompiled);

            public static readonly Regex StartHtmlLink = new Regex(@"^<a ", RegexOptions.IgnoreCase | RegexOptionCompiled);
            public static readonly Regex EndHtmlLink = new Regex(@"^<\/a>", RegexOptions.IgnoreCase | RegexOptionCompiled);
        }

        public static class Helper
        {
            public static readonly Regex EscapeWithEncode = new Regex(@"&", RegexOptionCompiled);
            public static readonly Regex EscapeWithoutEncode = new Regex(@"&(?!#?\w+;)", RegexOptionCompiled);

            public static readonly Regex Unescape = new Regex(@"&([#\w]+);", RegexOptionCompiled);

        }
    }
}
