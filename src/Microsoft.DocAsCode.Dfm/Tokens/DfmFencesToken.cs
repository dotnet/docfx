// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public abstract class DfmFencesToken : IMarkdownToken
    {
        public DfmFencesToken(IMarkdownRule rule, IMarkdownContext context, string name, string path, SourceInfo sourceInfo, string lang, string title, IDfmFencesBlockPathQueryOption pathQueryOption)
        {
            Rule = rule;
            Context = context;
            Path = path;
            Lang = lang;
            Name = name;
            Title = title;
            PathQueryOption = pathQueryOption;
            SourceInfo = sourceInfo;
        }

        public DfmFencesToken(IMarkdownRule rule, IMarkdownContext context, string name, string path, SourceInfo sourceInfo, string lang, string title, IDfmFencesBlockPathQueryOption pathQueryOption, string queryStringAndFragment)
            : this(rule, context, name, path, sourceInfo, lang, title, pathQueryOption)
        {
            QueryStringAndFragment = queryStringAndFragment;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Path { get; }

        public string Lang { get; }

        public string Name { get; }

        public string Title { get; }

        public string QueryStringAndFragment { get; }

        public IDfmFencesBlockPathQueryOption PathQueryOption { get; }

        public SourceInfo SourceInfo { get; }
    }
}
