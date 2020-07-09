// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class DocfxBuildTelemetryModel
    {
        public Dictionary<string, Dictionary<string, object>> FileMetadata { get; set; } = new Dictionary<string, Dictionary<string, object>>();

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();

        public bool ShouldSerializeFileMetadata() => false;
    }
}
