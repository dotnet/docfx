// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class FileMetadata
    {
        public string Layout { get; set; }

        public string Locale { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }

        public string Robots { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }
    }
}
