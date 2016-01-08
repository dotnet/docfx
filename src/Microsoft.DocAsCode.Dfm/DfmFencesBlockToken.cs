// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;

    public class DfmFencesBlockToken : IMarkdownToken
    {
        public DfmFencesBlockToken(IMarkdownRule rule, IMarkdownContext context, string name, string path, string rawMarkdown, string lang = null, string title = null, DfmFencesBlockPathQueryOption pathQueryOption = null)
        {
            Rule = rule;
            Context = context;
            Path = path;
            Lang = lang;
            Name = name;
            Title = title;
            PathQueryOption = pathQueryOption;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Path { get; }

        public string Lang { get; }

        public string Name { get; }

        public string Title { get; }

        public DfmFencesBlockPathQueryOption PathQueryOption { get; }

        public string RawMarkdown { get; set; }
    }
}
