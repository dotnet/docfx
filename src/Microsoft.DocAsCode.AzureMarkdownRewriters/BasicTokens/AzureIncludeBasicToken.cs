// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public abstract class AzureIncludeBasicToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Src { get; }
        public string Name { get; }
        public string Title { get; }
        public string Raw { get; }
        public SourceInfo SourceInfo { get; }
        public ImmutableArray<IMarkdownToken> Tokens { get; }

        protected AzureIncludeBasicToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, ImmutableArray<IMarkdownToken> tokens, string raw, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Src = src;
            Name = name;
            Title = title;
            Raw = raw;
            SourceInfo = sourceInfo;
            Tokens = tokens;
        }
    }
}