﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Newtonsoft.Json;

    public class VersionInfo
    {
        [JsonProperty("dest")]
        public string Destination { get; set; }
    }
}
