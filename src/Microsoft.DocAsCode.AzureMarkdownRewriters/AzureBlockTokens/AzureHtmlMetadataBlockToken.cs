// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Generic;

    using MarkdownLite;

    public class AzureHtmlMetadataBlockToken : IMarkdownToken
    {
        public AzureHtmlMetadataBlockToken(IMarkdownRule rule,
            IMarkdownContext context,
            IReadOnlyDictionary<string, string> properties,
            IReadOnlyDictionary<string, string> tags,
            SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Properties = properties;
            Tags = tags;
            SourceInfo = sourceInfo;
        }

        public IReadOnlyDictionary<string, string> Properties { get; set; }

        public IReadOnlyDictionary<string, string> Tags { get; set; }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public SourceInfo SourceInfo { get; }
    }
}
