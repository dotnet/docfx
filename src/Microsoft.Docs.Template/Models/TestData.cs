// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    [DataSchema]
    public sealed class TestData
    {
        public string Name { get; set; }

        public string FullName { get; set; }

        public string Description { get; set; }

        public string Summary { get; set; }

        public string InlineDescription { get; set; }

        public string Uid { get; set; }

        public string Xref { get; set; }

        public TestData Data { get; set; }

        public TestData[] Array { get; set; }
    }
}
