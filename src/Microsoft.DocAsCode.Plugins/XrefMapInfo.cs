// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using Newtonsoft.Json;

    public class XrefMapInfo
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
