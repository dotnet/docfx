// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureVideoBlockToken : IMarkdownToken
    {
        public AzureVideoBlockToken(IMarkdownRule rule, IMarkdownContext context, string videoId, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            VideoId = videoId;
            SourceInfo = sourceInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public SourceInfo SourceInfo { get; }

        public string VideoId { get; }
    }
}
