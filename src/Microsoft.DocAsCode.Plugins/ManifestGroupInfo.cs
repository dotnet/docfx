// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class ManifestGroupInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("dest")]
        public string Destination { get; set; }

        [JsonProperty("xrefmap")]
        public string XRefmap { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public ManifestGroupInfo(GroupInfo groupInfo)
        {
            if (groupInfo == null)
            {
                return;
            }

            Name = groupInfo.Name;
            Destination = groupInfo.Destination;
            Metadata = groupInfo.Metadata;
        }
    }
}