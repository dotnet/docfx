// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsInputModel
    {
        public JObject Metadata { get; set; }

        public List<TableOfContentsInputItem> Items { get; set; } = new List<TableOfContentsInputItem>();
    }
}
