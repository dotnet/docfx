// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmTextInlineRule : MarkdownTextInlineRule
    {
        private static readonly Regex _inlineTextRegex = new Regex(@"^[\s\S]+?(?=\S*@|\b_|[\\<!\[*`~\:]|\bhttps?:\/\/| {2,}\n|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        /// <summary>
        /// Override the one in MarkdownLite, difference is:
        /// If there is a `@` following `.`, `,`, `;`, `:`, `!`, `?` or whitespace, exclude it as it is a xref
        /// </summary>
        public override Regex Text => _inlineTextRegex;
    }
}
