// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class OutputFileInfo
    {
        [JsonProperty("relative_path")]
        public string RelativePath { get; set; }

        [JsonProperty("version_folder")]
        public string VersionFolder { get; set; }

        [JsonProperty("relative_path_from_version_folder")]
        public string RelativePathFromVersionFolder { get; set; }

        [JsonProperty("link_to_path")]
        public string LinkToPath { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
