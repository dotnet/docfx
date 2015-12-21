// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmYamlHeaderBlockToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Content { get; }
        public string RawMarkdown { get; set; }

        public DfmYamlHeaderBlockToken(IMarkdownRule rule, IMarkdownContext context, string content)
        {
            Rule = rule;
            Context = context;
            Content = content;
        }
    }
}
