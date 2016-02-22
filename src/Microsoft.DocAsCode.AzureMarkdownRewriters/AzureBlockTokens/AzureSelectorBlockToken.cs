// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using MarkdownLite;

    public class AzureSelectorBlockToken : IMarkdownToken
    {

        public AzureSelectorBlockToken(IMarkdownRule rule, IMarkdownContext context, string selectorType, string selectorConditions, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            SelectorType = selectorType;
            SelectorConditions = selectorConditions;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string SelectorType { get; }

        public string SelectorConditions { get; }

        public string RawMarkdown { get; set; }
    }
}
