// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmTextInlineRule : MarkdownTextInlineRule
    {
        private static readonly Regex _inlineTextRegex = new Regex(@"^[\s\S]+?(?=\S*@|[\\<!\[_*`]| {2,}\n|$)", RegexOptions.Compiled);

        /// <summary>
        /// Override the one in MarkdownLite, difference is:
        /// If there is a `@` following `.`, `,`, `;`, `:`, `!`, `?` or whitespace, exclude it as it is a xref
        /// </summary>
        public override Regex Text => _inlineTextRegex;
    }
}
