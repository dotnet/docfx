// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ECMA2Yaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class OpsDocsetConfig
    {
        public string DocsetName { get; private set; } = "";

        public PathString BuildSourceFolder { get; private set; }

        public bool OpenToPublicContributors { get; private set; }

        public string[] XrefQueryTags { get; private set; } = Array.Empty<string>();

        public Dictionary<string, string[]>? CustomizedTasks { get; private set; }

        [JsonProperty(nameof(JoinTOCPlugin))]
        public OpsJoinTocConfig[]? JoinTOCPlugin { get; private set; }

        [JsonProperty(nameof(ECMA2Yaml))]
        [JsonConverter(typeof(OneOrManyConverter))]
        public ECMA2YamlRepoConfig[]? ECMA2Yaml { get; private set; }
    }
}
