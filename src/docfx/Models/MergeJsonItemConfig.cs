// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;

    [Serializable]
    public class MergeJsonItemConfig
    {
        [JsonIgnore]
        public string BaseDirectory { get; set; }

        [JsonIgnore]
        public string OutputFolder { get; set; }

        [JsonProperty("content")]
        public FileMapping Content { get; set; }

        [JsonProperty("dest")]
        public string Destination { get; set; }

        [JsonProperty("globalMetadata")]
        [JsonConverter(typeof(JObjectDictionaryToObjectDictionaryConverter))]
        public Dictionary<string, object> GlobalMetadata { get; set; }

        /// <summary>
        /// Metadata that applies to some specific files.
        /// The key is the metadata name.
        /// For each item of the value:
        ///     The key is the glob pattern to match the files.
        ///     The value is the value of the metadata.
        /// </summary>
        [JsonProperty("fileMetadata")]
        public Dictionary<string, FileMetadataPairs> FileMetadata { get; set; }

        [JsonProperty("tocMetadata")]
        public ListWithStringFallback TocMetadata { get; set; }
    }
}
