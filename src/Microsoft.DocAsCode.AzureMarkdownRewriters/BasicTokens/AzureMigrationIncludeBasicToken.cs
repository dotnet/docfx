// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public abstract class AzureMigrationIncludeBasicToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Name { get; }
        public string Src { get; }
        public string Title { get; }
        public SourceInfo SourceInfo { get; }

        protected AzureMigrationIncludeBasicToken(IMarkdownRule rule, IMarkdownContext context, string name, string src, string title, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Name = name;
            Src = src;
            Title = title;
            SourceInfo = sourceInfo;
        }
    }
}