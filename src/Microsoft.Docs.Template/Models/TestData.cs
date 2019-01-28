// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    [DataSchema]
    public sealed class TestData
    {
        [XrefProperty]
        public string Name { get; set; }

        [XrefProperty]
        public string FullName { get; set; }

        [XrefProperty]
        public string Description { get; set; }

        [XrefProperty]
        [Markdown]
        public string Summary { get; set; }

        [XrefProperty]
        [InlineMarkdown]
        public string InlineDescription { get; set; }

        public string Uid { get; set; }

        [Xref]
        public string Xref { get; set; }

        public TestData Data { get; set; }

        public TestData[] Array { get; set; }
    }
}
