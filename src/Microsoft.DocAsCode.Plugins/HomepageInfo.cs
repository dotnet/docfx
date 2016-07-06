// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class HomepageInfo
    {
        [YamlMember(Alias = "tocPath")]
        [JsonProperty("tocPath")]
        public string TocPath { get; set; }

        [YamlMember(Alias = "homepage")]
        [JsonProperty("homepage")]
        public string Homepage { get; set; }
    }
}
