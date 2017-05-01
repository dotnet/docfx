// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class PreviewJsonConfig
    {
        [JsonProperty("references")]
        public Dictionary<string, string> References { get; set; }

        [JsonProperty("tocMetadataName")]
        public string TocMetadataName { get; set; }
    }
}
