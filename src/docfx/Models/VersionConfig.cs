// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class VersionConfig
    {
        [JsonProperty("dest")]
        public string Destination { get; set; }
    }
}
