// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownCodeBlockToken : IMarkdownToken
    {
        public MarkdownCodeBlockToken(IMarkdownRule rule, IMarkdownContext context, string code, string lang, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Code = code;
            Lang = lang;
            SourceInfo = sourceInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Code { get; }

        public string Lang { get; }

        public SourceInfo SourceInfo { get; }
    }
}
