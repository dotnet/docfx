// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using Newtonsoft.Json;

    public class HomepageInfo
    {
        [JsonProperty("tocPath")]
        public string TocPath { get; set; }

        [JsonProperty("homepage")]
        public string Homepage { get; set; }
    }
}
