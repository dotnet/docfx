// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Moniker
    {
        [JsonRequired]
        public string MonikerName { get; set; }

        [JsonRequired]
        public string ProductName { get; set; }

        [JsonRequired]
        public int Order { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();
    }
}
