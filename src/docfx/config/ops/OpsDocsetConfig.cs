// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    }
}
