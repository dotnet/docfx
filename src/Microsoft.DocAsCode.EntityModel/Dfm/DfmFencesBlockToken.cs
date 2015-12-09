// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;

    public class DfmFencesBlockToken : IMarkdownToken
    {
        public DfmFencesBlockToken(IMarkdownRule rule, string name, string path, string lang = null, string title = null)
        {
            Rule = rule;
            Path = path;
            Lang = lang;
            Name = name;
            Title = title;
        }

        public IMarkdownRule Rule { get; }

        public string Path { get; }

        public string Lang { get; }

        public string Name { get; }

        public string Title { get; }

        public string RawMarkdown { get; set; }
    }
}
