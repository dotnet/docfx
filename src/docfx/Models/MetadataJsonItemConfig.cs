// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class MetadataJsonItemConfig
    {
        [JsonProperty("src")]
        public FileMapping Source { get; set; }

        [JsonProperty("dest")]
        public string Destination { get; set; }

        [JsonProperty("force")]
        public bool? Force { get; set; }

        [JsonProperty("shouldSkipMarkup")]
        public bool? ShouldSkipMarkup { get; set; }

        [JsonProperty("raw")]
        public bool? Raw { get; set; }

        [JsonProperty("references")]
        public FileMapping References { get; set; }

        [JsonProperty("filter")]
        public string FilterConfigFile { get; set; }

        [JsonProperty("globalNamespaceId")]
        public string GlobalNamespaceId { get; set; }

        [JsonProperty("useCompatibilityFileName")]
        public bool? UseCompatibilityFileName { get; set; }

        /// <summary>
        /// An optional set of MSBuild properties used when interpreting project files. These
        ///  are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt;
        ///  command line argument.
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, string> MSBuildProperties { get; set; }

        [JsonProperty("disableGitFeatures")]
        public bool DisableGitFeatures { get; set; }

        [JsonProperty("codeSourceBasePath")]
        public string CodeSourceBasePath { get; set; }

        [JsonProperty("disableDefaultFilter")]
        public bool DisableDefaultFilter { get; set; }
    }
}
