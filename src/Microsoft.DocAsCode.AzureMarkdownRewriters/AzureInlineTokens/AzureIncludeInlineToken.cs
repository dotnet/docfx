// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureIncludeInlineToken : AzureIncludeBasicToken
    {
        public AzureIncludeInlineToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, string raw, string rawMarkdown)
            : base(rule, context, src, name, title, raw, rawMarkdown)
        {
        }
    }
}
