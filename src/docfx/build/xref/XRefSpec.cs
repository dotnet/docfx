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

        // not into output for now
        [JsonIgnore]
        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public string Href { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        public string GetName() => GetXrefPropertyValue("name");

        public string GetXrefPropertyValue(string propertyName)
        {
            if (propertyName is null)
                return null;

            if (ExtensionData.TryGetValue<JValue>(propertyName, out var v))
            {
                return v.Value is string str ? str : null;
            }
            return null;
        }

        public XrefSpec Clone()
        {
            var spec = new XrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };

            spec.ExtensionData.Merge(ExtensionData);
            return spec;
        }
    }
}
