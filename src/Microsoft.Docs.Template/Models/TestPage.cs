// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    [PageSchema]
    public sealed class TestPage
    {
        public string Description { get; set; }

        public string InlineDescription { get; set; }

        public string Html { get; set; }

        public string Href { get; set; }
    }
}
