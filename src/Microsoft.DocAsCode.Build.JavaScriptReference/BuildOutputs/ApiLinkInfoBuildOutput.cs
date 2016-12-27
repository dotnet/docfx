// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiLinkInfoBuildOutput
    {
        [YamlMember(Alias = "linkType")]
        [JsonProperty("linkType")]
        public LinkType LinkType { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public ApiReferenceBuildOutput Type { get; set; }

        [YamlMember(Alias = "url")]
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
