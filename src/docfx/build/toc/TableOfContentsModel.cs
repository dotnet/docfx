// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsModel
    {
        public TableOfContentsMetadata Metadata { get; set; } = new TableOfContentsMetadata();

        public List<TableOfContentsItem> Items { get; set; } = new List<TableOfContentsItem>();

        [JsonExtensionData(WriteData = false)]
        public JObject ExtensionData { get; } = new JObject();
    }
}
