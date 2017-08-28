﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    using Microsoft.DocAsCode.MarkdownLite;

    public abstract class DfmFencesToken : IMarkdownToken
    {
        [Obsolete]
        public DfmFencesToken(IMarkdownRule rule, IMarkdownContext context, string name, string path, SourceInfo sourceInfo, string lang, string title, IDfmFencesBlockPathQueryOption pathQueryOption)
            : this(rule, context, name, path, sourceInfo, lang, title, pathQueryOption, null) { }

        public DfmFencesToken(IMarkdownRule rule, IMarkdownContext context, string name, string path, SourceInfo sourceInfo, string lang, string title, IDfmFencesBlockPathQueryOption pathQueryOption, string queryStringAndFragment)
        {
            Rule = rule;
            Context = context;
            Path = path;
            Lang = lang;
            Name = name;
            Title = title;
            PathQueryOption = pathQueryOption;
            SourceInfo = sourceInfo;
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
