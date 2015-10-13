// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    internal static class Regexes
    {
        public static readonly Regex Noop = new Regex("", RegexOptions.Compiled);

        public static class Block
        {
            public static readonly Regex Newline = new Regex(@"^\n+", RegexOptions.Compiled);
            public static readonly Regex Code = new Regex(@"^( {4}[^\n]+\n*)+", RegexOptions.Compiled);
            public static readonly Regex Hr = new Regex(@"^( *[-*_]){3,} *(?:\n+|$)", RegexOptions.Compiled);
            public static readonly Regex Heading = new Regex(@"^ *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)", RegexOptions.Compiled);
            public static readonly Regex LHeading = new Regex(@"^([^\n]+)\n *(=|-){2,} *(?:\n+|$)", RegexOptions.Compiled);
            public static readonly Regex Blockquote = new Regex(@"^( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+", RegexOptions.Compiled);
            public static readonly Regex List = new Regex(@"^( *)((?:[*+-]|\d+\.)) [\s\S]+?(?:\n+(?=\1?(?:[-*_] *){3,}(?:\n+|$))|\n+(?= *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! )(?!\1(?:[*+-]|\d+\.) )\n*|\s*$)", RegexOptions.Compiled);
            public static readonly Regex Html = new Regex(@"^ *(?:<!--[\s\S]*?-->|<((?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b)[\s\S]+?<\/\1>|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b(?:""[^""]*""|'[^']*'|[^'"">])*?>) *(?:\n{2,}|\s*$)", RegexOptions.Compiled);
            public static readonly Regex Def = new Regex(@"^ *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)", RegexOptions.Compiled);
            public static readonly Regex Paragraph = new Regex(@"^((?:[^\n]+\n?(?!( *[-*_]){3,} *(?:\n+|$)| *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)|([^\n]+)\n *(=|-){2,} *(?:\n+|$)|( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b| *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)))+)\n*", RegexOptions.Compiled);
            public static readonly Regex Text = new Regex(@"^[^\n]+", RegexOptions.Compiled);
            public static readonly Regex Bullet = new Regex(@"(?:[*+-]|\d+\.)", RegexOptions.Compiled);
            public static readonly Regex Item = new Regex(@"^( *)((?:[*+-]|\d+\.)) [^\n]*(?:\n(?!\1(?:[*+-]|\d+\.) )[^\n]*)*", RegexOptions.Multiline | RegexOptions.Compiled);

            public static class Gfm
            {
                public static readonly Regex Fences = new Regex(@"^ *(`{3,}|~{3,}) *(\S+)? *\n([\s\S]+?)\s*\1 *(?:\n+|$)", RegexOptions.Compiled);
                public static readonly Regex Paragraph = new Regex(@"^((?:[^\n]+\n?(?! *(`{3,}|~{3,}) *(\S+)? *\n([\s\S]+?)\s*\2 *(?:\n+|$)|( *)((?:[*+-]|\d+\.)) [\s\S]+?(?:\n+(?=\3?(?:[-*_] *){3,}(?:\n+|$))|\n+(?= *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! )(?!\1(?:[*+-]|\d+\.) )\n*|\s*$)|( *[-*_]){3,} *(?:\n+|$)| *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)|([^\n]+)\n *(=|-){2,} *(?:\n+|$)|( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b| *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)))+)\n*", RegexOptions.Compiled);
                public static readonly Regex Heading = new Regex(@"^ *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)", RegexOptions.Compiled);
            }

            public static class Tables
            {
                public static readonly Regex NpTable = new Regex(@"^ *(\S.*\|.*)\n *([-:]+ *\|[-| :]*)\n((?:.*\|.*(?:\n|$))*)\n*", RegexOptions.Compiled);
                public static readonly Regex Table = new Regex(@"^ *\|(.+)\n *\|( *[-:]+[-| :]*)\n((?: *\|.*(?:\n|$))*)\n*", RegexOptions.Compiled);
            }
        }

        public static class Inline
        {
            public static readonly Regex Escape = new Regex(@"^\\([\\`*{}\[\]()#+\-.!_>])", RegexOptions.Compiled);
            public static readonly Regex AutoLink = new Regex(@"^<([^ >]+(@|:\/)[^ >]+)>", RegexOptions.Compiled);
            public static readonly Regex Tag = new Regex(@"^<!--[\s\S]*?-->|^<\/?\w+(?:""[^""]*""|'[^']*'|[^'"">])*?>", RegexOptions.Compiled);
            public static readonly Regex Link = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([\s\S]*?)>?(?:\s+['""]([\s\S]*?)['""])?\s*\)", RegexOptions.Compiled);
            public static readonly Regex RefLink = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\s*\[([^\]]*)\]", RegexOptions.Compiled);
            public static readonly Regex NoLink = new Regex(@"^!?\[((?:\[[^\]]*\]|[^\[\]])*)\]", RegexOptions.Compiled);
            public static readonly Regex Strong = new Regex(@"^__([\s\S]+?)__(?!_)|^\*\*([\s\S]+?)\*\*(?!\*)", RegexOptions.Compiled);
            public static readonly Regex Em = new Regex(@"^\b_((?:__|[\s\S])+?)_\b|^\*((?:\*\*|[\s\S])+?)\*(?!\*)", RegexOptions.Compiled);
            public static readonly Regex Code = new Regex(@"^(`+)\s*([\s\S]*?[^`])\s*\1(?!`)", RegexOptions.Compiled);
            public static readonly Regex Br = new Regex(@"^ {2,}\n(?!\s*$)", RegexOptions.Compiled);
            public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`]| {2,}\n|$)", RegexOptions.Compiled);

            public static class Pedantic
            {
                public static readonly Regex Strong = new Regex(@"^__(?=\S)([\s\S]*?\S)__(?!_)|^\*\*(?=\S)([\s\S]*?\S)\*\*(?!\*)", RegexOptions.Compiled);
                public static readonly Regex Em = new Regex(@"^_(?=\S)([\s\S]*?\S)_(?!_)|^\*(?=\S)([\s\S]*?\S)\*(?!\*)", RegexOptions.Compiled);
            }

            public static class Gfm
            {
                public static readonly Regex Escape = new Regex(@"^\\([\\`*{}\[\]()#+\-.!_>~|])", RegexOptions.Compiled);
                public static readonly Regex Url = new Regex(@"^(https?:\/\/[^\s<]+[^<.,:;""')\]\s])", RegexOptions.Compiled);
                public static readonly Regex Del = new Regex(@"^~~(?=\S)([\s\S]*?\S)~~", RegexOptions.Compiled);
                public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`~]|https?:\/\/| {2,}\n|$)", RegexOptions.Compiled);
            }

            public static class Breaks
            {
                public static readonly Regex Br = new Regex(@"^ *\n(?!\s*$)", RegexOptions.Compiled);
                public static readonly Regex Text = new Regex(@"^[\s\S]+?(?=[\\<!\[_*`~]|https?:\/\/| *\n|$)", RegexOptions.Compiled);
            }

            public static class Smartypants
            {
                public static readonly Regex OpeningSingles = new Regex(@"(^|[-\u2014/(\[{""\s])'", RegexOptions.Compiled);
                public static readonly Regex OpeningDoubles = new Regex(@"(^|[-\u2014/(\[{\u2018\s])""", RegexOptions.Compiled);
            }
        }

        public static class Lexers
        {
            public static readonly Regex NormalizeNewLine = new Regex(@"\r\n|\r", RegexOptions.Compiled);
            public static readonly Regex WhiteSpaceLine = new Regex(@"^ +$", RegexOptions.Multiline | RegexOptions.Compiled);
            public static readonly Regex WhiteSpaces = new Regex(@"\s+", RegexOptions.Compiled);

            public static readonly Regex LeadingWhiteSpaces = new Regex(@"^ {4}", RegexOptions.Multiline | RegexOptions.Compiled);
            public static readonly Regex TailingEmptyLines = new Regex(@"\n+$", RegexOptions.Compiled);

            public static readonly Regex UselessTableHeader = new Regex(@"^ *| *\| *$", RegexOptions.Compiled);
            public static readonly Regex UselessTableAlign = new Regex(@"^ *|\| *$", RegexOptions.Compiled);
            public static readonly Regex UselessGfmTableCell = new Regex(@"(?: *\| *)?\n$", RegexOptions.Compiled);
            public static readonly Regex EmptyGfmTableCell = new Regex(@"^ *\| *| *\| *$", RegexOptions.Compiled);
            public static readonly Regex TableSplitter = new Regex(@" *\| *", RegexOptions.Compiled);
            public static readonly Regex EndWithNewLine = new Regex(@"\n$", RegexOptions.Compiled);
            public static readonly Regex TableAlignRight = new Regex(@"^ *-+: *$", RegexOptions.Compiled);
            public static readonly Regex TableAlignCenter = new Regex(@"^ *:-+: *$", RegexOptions.Compiled);
            public static readonly Regex TableAlignLeft = new Regex(@"^ *:-+ *$", RegexOptions.Compiled);

            public static readonly Regex LeadingBlockquote = new Regex(@"^ *> ?", RegexOptions.Multiline | RegexOptions.Compiled);
            public static readonly Regex LeadingBullet = new Regex(@"^ *([*+-]|\d+\.) +", RegexOptions.Compiled);

            public static readonly Regex StartHtmlLink = new Regex(@"^<a ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            public static readonly Regex EndHtmlLink = new Regex(@"^<\/a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public static class Helper
        {
            public static readonly Regex EscapeWithEncode = new Regex(@"&", RegexOptions.Compiled);
            public static readonly Regex EscapeWithoutEncode = new Regex(@"&(?!#?\w+;)", RegexOptions.Compiled);

            public static readonly Regex Unescape = new Regex(@"&([#\w]+);", RegexOptions.Compiled);

        }
    }
}
