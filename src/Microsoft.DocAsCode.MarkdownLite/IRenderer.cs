// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public interface IRenderer
    {
        Options Options { get; set; }
        StringBuffer Blockquote(StringBuffer quote);
        StringBuffer Br();
        StringBuffer Code(string code, string lang, bool escaped);
        StringBuffer CodeSpan(StringBuffer text);
        StringBuffer Del(StringBuffer text);
        StringBuffer Em(StringBuffer text);
        StringBuffer Heading(StringBuffer text, int level, string raw);
        StringBuffer Hr();
        StringBuffer Html(StringBuffer html);
        StringBuffer Image(StringBuffer href, StringBuffer title, StringBuffer text);
        StringBuffer Link(StringBuffer href, StringBuffer title, StringBuffer text);
        StringBuffer List(StringBuffer body, bool ordered);
        StringBuffer ListItem(StringBuffer text);
        StringBuffer Paragraph(StringBuffer text);
        StringBuffer Strong(StringBuffer text);
        StringBuffer Table(StringBuffer header, StringBuffer body);
        StringBuffer TableCell(StringBuffer content, TableCellFlags flags);
        StringBuffer TableRow(StringBuffer content);
        StringBuffer Text(StringBuffer text);
    }
}
