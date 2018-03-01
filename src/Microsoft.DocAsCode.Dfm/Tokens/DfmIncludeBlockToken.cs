// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmIncludeBlockToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Src { get; }
        public string Name { get; }
        public string Title { get; }
        [Obsolete]
        public string Raw => SourceInfo.Markdown;
        public SourceInfo SourceInfo { get; }

        [Obsolete]
        public DfmIncludeBlockToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, string raw, SourceInfo sourceInfo)
            : this(rule, context, src, name, title, sourceInfo) { }

        public DfmIncludeBlockToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Src = src;
            Name = name;
            Title = title;
            SourceInfo = sourceInfo;
        }
    }
}
