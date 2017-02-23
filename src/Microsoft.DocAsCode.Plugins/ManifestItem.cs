// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class ManifestItem
    {
        [JsonProperty("type")]
        public string DocumentType { get; set; }

        [JsonProperty("source_relative_path")]
        public string SourceRelativePath { get; set; }

        [JsonProperty("output")]
        public Dictionary<string, OutputFileInfo> OutputFiles { get; set; }

        [JsonProperty("version")]
        public string VersionName { get; set; }

        [JsonProperty("is_incremental")]
        public bool IsIncremental { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public ManifestItem Clone(bool isIncremental, string sourceRelativePath)
        {
            var cloned = Clone();
            cloned.IsIncremental = isIncremental;
            cloned.SourceRelativePath = sourceRelativePath;
            return cloned;
        }

        public ManifestItem Clone()
        {
            return (ManifestItem)MemberwiseClone();
        }
    }
}
