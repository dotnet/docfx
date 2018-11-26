// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    internal static class Regexes
    {
        private const RegexOptions RegexOptionCompiled = RegexOptions.Compiled;
        private static readonly TimeSpan RegexTimeOut = TimeSpan.FromSeconds(10);

        public static class Block
        {
            [Obsolete]
            public static readonly Regex Newline = new Regex(@"^\n+", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Code = new Regex(@"^ {4}.+(?:\n+ {4}.+)*\n?", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Hr = new Regex(@"^( *[-*_]){3,} *(?:\n+|$)", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Heading = new Regex(@"^ *(#{1,6}) *([^\n]+?)(?: +#*)? *(?:\n+|$)", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex LHeading = new Regex(@"^([^\n]+)\n *(=|-){2,} *(?:\n+|$)", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Blockquote = new Regex(@"^( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex UnorderList = new Regex(@"^( *)([*+-]) [\s\S]+?(?:\n+(?=([^\n]+)\n(=|-){2,} *(?:\n+|$))|\n+(?=\1?(?:[-*_] *){3,}(?:\n+|$))|\n+(?=\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! (?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])?))(?!\s*\1([*+-]) )\n*|\s*$)", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex OrderList = new Regex(@"^( *)(\d+\.) [\s\S]+?(?:\n+(?=([^\n]+)\n(=|-){2,} *(?:\n+|$))|\n+(?=\1?(?:[-*_] *){3,}(?:\n+|$))|\n+(?=\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! (?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])?))(?!\s*\1(\d+\.) )\n*|\s*$)", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Html = new Regex(@"^ *(?:<!--(?:[^-]|-(?!->))*-->|<((?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:)(?!:\/|[^\w\s@]*@)\b)[\s\S]+?<\/\1>|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b(?!:)(?:""[^""]*""|'[^']*'|[^'"">])*?>) *(?:\n{2,}|\s*$)", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Def = new Regex(@"^ *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex PreElement = new Regex(@"^ *\<pre(?=[ \n>])[\s\S]*?\<\/pre[ \n]*\>.*\n*", RegexOptionCompiled | RegexOptions.IgnoreCase, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Paragraph = new Regex(@"^((?:[^\n]+\n?(?!( *[-*_]){3,} *(?:\n+|$)| *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)|([^\n]+)\n *(=|-){2,} *(?:\n+|$)|( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b| *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)))+)\n*", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex Text = new Regex(@"^[^\n]+\n?", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Bullet = new Regex(@"(?:[*+-]|\d+\.)", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Item = new Regex(@"^( *)((?:[*+-]|\d+\.)) [^\n]*(?:\n(?!\1(?:[*+-]|\d+\.) )[^\n]*)*", RegexOptions.Multiline | RegexOptionCompiled, RegexTimeOut);

            public static class Gfm
            {
                [Obsolete]
                public static readonly Regex Fences = new Regex(@"^(?:(?> *(`{3,}) *(\S+)? *\n)((?:(?>.*)\n)*?) *\1`*|(?> *(~{3,}) *(\S+)? *\n)((?:(?>.*)\n)*?) *\1~*) *(?:\n+|$)", RegexOptionCompiled, RegexTimeOut);
                [Obsolete]
                public static readonly Regex Paragraph = new Regex(@"^((?:[^\n]+\n?(?! *(`{3,}|~{3,}) *(\S+)? *\n([\s\S]+?)\s*\2 *(?:\n+|$)|( *)((?:[*+-]|\d+\.)) [\s\S]+?(?:\n+(?=\5?(?:[-*_] *){3,}(?:\n+|$))|\n+(?= *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! )(?!\5(?:[*+-]|\d+\.) )\n*|\s*$)|( *[-*_]){3,} *(?:\n+|$)| *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)|([^\n]+)\n *(=|-){2,} *(?:\n+|$)|( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b| *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)))+)\n*", RegexOptionCompiled, RegexTimeOut);
                [Obsolete]
                public static readonly Regex Heading = new Regex(@"^ *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)", RegexOptionCompiled, RegexTimeOut);
                [Obsolete]
                public static readonly Regex HtmlComment = new Regex(@"^(?:<!--(?:[^-]|-(?!->))*-->) *(?:\n|$)", RegexOptionCompiled, RegexTimeOut);
            }

            public static class Tables
            {
                [Obsolete]
                public static readonly Regex NpTable = new Regex(@"^ *\|?(.+)\n *\|? *([-:]+ *\|[-| :]*)\n((?:.*\|.*(?:\n|$))*)\n*", RegexOptionCompiled, RegexTimeOut);
                [Obsolete]
                public static readonly Regex Table = new Regex(@"^ *\|(.+)\n *\|( *[-:]+[-| :]*)\n((?: *\|.*(?:\n|$))*)\n*", RegexOptionCompiled, RegexTimeOut);
            }
        }

        public static class Inline
        {
            public static readonly Regex Escape = new Regex(@"^\\([\\`*{}\[\]()#+\-.!_>])", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Comment = new Regex(@"^<!--(?:[^-]|-(?!->))*-->", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex AutoLink = new Regex(@"^<([^ >]+(@|:\/)[^ >]+)>", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex PreElement = new Regex(@"^\<pre\>[\s\S]*?\</pre\>", RegexOptionCompiled | RegexOptions.IgnoreCase, RegexTimeOut);
            public static readonly Regex Tag = new Regex(@"^<\/?[A-Za-z][A-Za-z0-9\-]*(?:\s+[A-Za-z_][A-Za-z0-9\-_]*(?:\:[A-Za-z_][A-Za-z0-9\-_]*)?(?:\s*=\s*(?:""[^""]*""|'[^']*'))?)*\s*\/?>", RegexOptionCompiled, RegexTimeOut);
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
            ///         [^()\s]                             any chararacter but '(', ')' or white spaces
            ///         |                                   or
            ///         \((?<DEPTH>)                        '(' with depth++
            ///         |                                   or
            ///         \)(?<-DEPTH>)                       ')' with depth--
            ///     )                                       end non-capturing group
            ///     *?                                      lazy 0~
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
            public static readonly Regex Link = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\s*\(\s*<?((?:[^()\s]|\((?<DEPTH>)|\)(?<-DEPTH>))*?(?(DEPTH)(?!)))>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex RefLink = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\s*\[\s*([^\]]*?)\s*\]", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex NoLink = new Regex(@"^!?\[\s*((?:\[[^\]]*?\]|[^\[\]])*?)\s*\]", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Strong = new Regex(@"^__([\s\S]+?)__(?!_)|^\*\*([\s\S]+?)\*\*(?!\*)", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Em = new Regex(@"^_((?:__|[\s\S])+?)_\b|^\*((?:\*{2,}|[^\\*]|\\[\s\S])+?)\*(?!\*)", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Code = new Regex(@"^(`+)\s*([\s\S]*?[^`])\s*\1(?!`)", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Br = new Regex(@"^ {2,}\n(?!\s*$)", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex EscapedText = new Regex(@"^\\([!""#$%&'()*+,.:;<=>?@[^_`{|}~\-\/\\\]])", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[*`]|\b_| {2,}\n|$)", RegexOptionCompiled, RegexTimeOut);

            public static class Pedantic
            {
                public static readonly Regex Strong = new Regex(@"^__(?=\S)([\s\S]*?\S)__(?!_)|^\*\*(?=\S)([\s\S]*?\S)\*\*(?!\*)", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Em = new Regex(@"^_(?=\S)([\s\S]*?\S)_(?!_)|^\*(?=\S)([\s\S]*?\S)\*(?!\*)", RegexOptionCompiled, RegexTimeOut);
            }

            public static class Gfm
            {
                public static readonly Regex Escape = new Regex(@"^\\([\\`*{}\[\]()#+\-.!_>~|])", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Url = new Regex(@"^(https?:\/\/[^\s<]+[^<.,:;""')\]\s])", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Del = new Regex(@"^~~(?=\S)([\s\S]*?\S)~~", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex StrongEm = new Regex(@"^(\**?)\*{3}(?!\*)(?=\S)([\s\S]*?\S)\*([\s\S]*?(?<=\S))?\*{2}", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Strong = new Regex(@"^__((?:_|(?>[^_]+))+?)__\b|^\*{2}(?!\*|\s)((?:[^*]|(?<=\s)\*{2,}|(?<!\*)\*(?!\*))+)(?<!\*|\s)\*{2}", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Em = new Regex(@"^_((?:_|(?>[^_]+))+?)_\b|^\*(\**(?!\s)(?:[^\\*]|\\[\s\S]|(?<=\s)\*+)+(?<!\s|\*))\*", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Emoji = new Regex(@"^\:([a-z0-9_\+\-]+)\:", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[*`~\:]|\b_|\bhttps?:\/\/| {2,}\n|$)", RegexOptionCompiled, RegexTimeOut);
            }

            public static class Breaks
            {
                public static readonly Regex Br = new Regex(@"^ *\n(?!\s*$)", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[*`~]|\b_|\bhttps?:\/\/| *\n|$)", RegexOptionCompiled, RegexTimeOut);
            }

            public static class Smartypants
            {
                public static readonly Regex OpeningSingles = new Regex(@"(^|[-\u2014/(\[{""\s])'", RegexOptionCompiled, RegexTimeOut);
                public static readonly Regex OpeningDoubles = new Regex(@"(^|[-\u2014/(\[{\u2018\s])""", RegexOptionCompiled, RegexTimeOut);
            }
        }

        public static class Lexers
        {
            public static readonly Regex NormalizeNewLine = new Regex(@"\r\n|\r", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex WhiteSpaceLine = new Regex(@"^ +$", RegexOptions.Multiline | RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex WhiteSpaces = new Regex(@"\s+", RegexOptionCompiled, RegexTimeOut);

            public static readonly Regex LeadingWhiteSpaces = new Regex(@"^ {4}", RegexOptions.Multiline | RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex TailingEmptyLine = new Regex(@"\n$", RegexOptionCompiled, RegexTimeOut);

            public static readonly Regex UselessTableHeader = new Regex(@"^ *|(?> *)\|? *$", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex UselessTableAlign = new Regex(@"^ *|\|? *$", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex UselessTableRow = new Regex(@"^ *\| *|(?> *)\|? *$", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex UselessGfmTableCell = new Regex(@"(?: *\| *)?\n$", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex TableSplitter = new Regex(@" *(?<!\\)\| *", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex EndWithNewLine = new Regex(@"\n$", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex TableAlignRight = new Regex(@"^ *-+: *$", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex TableAlignCenter = new Regex(@"^ *:-+: *$", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex TableAlignLeft = new Regex(@"^ *:-+ *$", RegexOptionCompiled, RegexTimeOut);

            public static readonly Regex LeadingBlockquote = new Regex(@"^ *> ?", RegexOptions.Multiline | RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex LeadingBullet = new Regex(@"^ *([*+-]|\d+\.) +", RegexOptionCompiled, RegexTimeOut);

            public static readonly Regex StartHtmlLink = new Regex(@"^<a [\s\S]*(?<!\/)>$", RegexOptions.IgnoreCase | RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex EndHtmlLink = new Regex(@"^<\/a>", RegexOptions.IgnoreCase | RegexOptionCompiled, RegexTimeOut);
        }

        public static class Helper
        {
            public static readonly Regex HtmlEscapeWithEncode = new Regex(@"&", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex HtmlEscapeWithoutEncode = new Regex(@"&(?!#?\w+;)", RegexOptionCompiled, RegexTimeOut);

            public static readonly Regex HtmlUnescape = new Regex(@"&([#\w]+);", RegexOptionCompiled, RegexTimeOut);

            public static readonly Regex MarkdownUnescape = new Regex(@"\\([!""#$%&'()*+,.:;<=>?@[^_`{|}~\-\/\\\]])", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex MarkdownEscape = new Regex(@"[!""'()*+:<>@[^_`{|}~\-\]]", RegexOptionCompiled, RegexTimeOut);
            public static readonly Regex MarkdownHrefEscape = new Regex(@"[()\\\""\']", RegexOptionCompiled, RegexTimeOut);

            [Obsolete]
            public static readonly Regex LegacyMarkdownUnescape = new Regex(@"\\([\\`*{}\[\]()#+\-.!_>@])", RegexOptionCompiled, RegexTimeOut);
            [Obsolete]
            public static readonly Regex LegacyMarkdownEscape = new Regex(@"[\\()\[\]]", RegexOptionCompiled, RegexTimeOut);
        }
    }
}
