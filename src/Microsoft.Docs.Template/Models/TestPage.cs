// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    [PageSchema]
    public sealed class TestPage
    {
        [Markdown]
        public string Description { get; set; }

        [InlineMarkdown]
        public string InlineDescription { get; set; }

        [Html]
        public string Html { get; set; }

        [Href]
        public string Href { get; set; }
    }
}
