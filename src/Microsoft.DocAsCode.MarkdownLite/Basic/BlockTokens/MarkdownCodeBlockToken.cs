// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownCodeBlockToken : IMarkdownToken
    {
        public MarkdownCodeBlockToken(IMarkdownRule rule, string code, string lang = null)
        {
            Rule = rule;
            Code = code;
            Lang = lang;
        }

        public IMarkdownRule Rule { get; }

        public string Code { get; }

        public string Lang { get; }

        public string RawMarkdown { get; set; }
    }
}
