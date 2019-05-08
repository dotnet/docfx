// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class LegacyManifestItem
    {
        // published url relative to site base path
        public string AssetId { get; set; }

        // rource path relative to source repo root
        public string Original { get; set; }

        // source path relative to source base path
        public string SourceRelativePath { get; set; }

        public string OriginalType { get; set; }

        public string Type { get; set; }

        public LegacyManifestOutput Output { get; set; }

        // tell ops to use plugin for normalization
        public bool SkipNormalization { get; set; }

        public bool SkipSchemaCheck { get; set; }

        public string Group { get; set; }
    }
}
