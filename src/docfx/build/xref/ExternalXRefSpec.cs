// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ExternalXrefSpec : IXrefSpec
    {
        public string Uid { get; set; }

        public string Href { get; set; }

        [JsonIgnore]
        public string[] Monikers { get; set; } = Array.Empty<string>();

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        Document IXrefSpec.DeclairingFile => null;

        JToken IXrefSpec.GetXrefProperty(string propertyName) => ExtensionData[propertyName];
    }
}
