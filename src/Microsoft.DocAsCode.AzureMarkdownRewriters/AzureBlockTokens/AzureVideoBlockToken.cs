// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureVideoBlockToken : IMarkdownToken
    {
        public AzureVideoBlockToken(IMarkdownRule rule, IMarkdownContext context, string videoId, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            VideoId = videoId;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string RawMarkdown { get; set; }

        public string VideoId { get; }
    }
}
