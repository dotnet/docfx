// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class TableOfContentsMetadata
    {
        public List<string> Monikers { get; set; }

        [JsonProperty(PropertyName = "monikerRange")]
        public string MonikerRange { get; set; }

        [JsonProperty(PropertyName = "pdf_absolute_path")]
        public string PdfAbsolutePath { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }

        public bool ShouldSerializeMonikerRange() => false;
    }
}
