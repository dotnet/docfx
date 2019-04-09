// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class TableOfContentsModel
    {
        public TableOfContentsMetadata Metadata { get; set; } = new TableOfContentsMetadata();

        [MinLength(1)]
        public List<TableOfContentsItem> Items { get; set; } = new List<TableOfContentsItem>();

        [JsonExtensionData(WriteData = false)]
        public JObject ExtensionData { get; set; }
    }
}
