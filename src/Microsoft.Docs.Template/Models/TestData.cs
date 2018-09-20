// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    [DataSchema]
    public sealed class TestData
    {
        public string Name { get; set; }

        public string FullName { get; set; }

        public string Description { get; set; }

        [Uid]
        public string Uid { get; set; }

        [Xref]
        public string Xref { get; set; }
    }
}
