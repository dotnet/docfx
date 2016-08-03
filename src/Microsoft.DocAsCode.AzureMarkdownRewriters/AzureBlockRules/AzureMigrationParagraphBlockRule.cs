// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMigrationParagraphBlockRule : GfmParagraphBlockRule
    {
        public override string Name => "AzureMigrationParagraph";

        private static readonly Regex _paragraph = new Regex(@"^((?:[^\n]+\n?(?! *(`{3,}|~{3,}) *(\S+)? *\n([\s\S]+?)\s*\2 *(?:\n+|$)| *\[AZURE.VIDEO\s*([^\]]*?)\s*\](?:\n|$)| *\[(AZURE.NOTE|AZURE.WARNING|AZURE.TIP|AZURE.IMPORTANT|AZURE.CAUTION)\] *\n?.*(?:\n|$)| *\[(AZURE.SELECTOR|AZURE.SELECTOR-LIST)( *\((.*?)\))?\] *(?:\n|$)| *\[\!div( +(`?)(.*?)\11)?\]\s*(?:\n|$)|\[!(code(-((\w|-)+))?)\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([\s\S]*?)>?(?:\s+(['""])([\s\S]*?)\16)?\s*\)\]\s*(\n|$)|( *)((?:[*+-]|\d+\.)) [\s\S]+?(?:\n+(?=\22?(?:[-*_] *){3,}(?:\n+|$))|\n+(?= *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))|\n{2,}(?! )(?!\22(?:[*+-]|\d+\.) )\n*|\s*$)|( *[-*_]){3,} *(?:\n+|$)| *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)|([^\n]+)\n *(=|-){2,} *(?:\n+|$)|( *>[^\n]+(\n(?! *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$))[^\n]+)*\n*)+|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b| *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +[""(]([^\n]+)["")])? *(?:\n+|$)))+)\n*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override Regex Paragraph
        {
            get
            {
                return _paragraph;
            }
        }
    }
}
