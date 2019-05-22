// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class Moniker
    {
        public string MonikerName { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int Order { get; set; } = 0;

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();
    }
}
