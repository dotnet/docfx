// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class GroupConfig
    {
        [JsonProperty("dest")]
        public string Destination { get; set; }

        /// <summary>
        /// The Root TOC Path used for navbar in current group, relative to output root.
        /// If not set, will use the toc in output root in current group if exists.
        /// </summary>
        [JsonProperty("rootTocPath")]
        public string RootTocPath { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
