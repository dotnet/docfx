// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class XrefSpec
    {
        public string Uid { get; set; }

        [JsonIgnore]
        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public string Href { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        [JsonIgnore]
        internal Document File { get; set; }

        public string GetName() => GetXrefPropertyValue("name");

        public string GetXrefPropertyValue(string property)
            => ExtensionData.TryGetValue<JValue>(property, out var v) && v.Value is string str ? str : null;
    }
}
