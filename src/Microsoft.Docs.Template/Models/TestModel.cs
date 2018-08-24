// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    [DataSchema]
    public class TestModel
    {
        [Markdown]
        public string Description { get; set; }

        [InlineMarkdown]
        public string InlineDescription { get; set; }
    }
}
