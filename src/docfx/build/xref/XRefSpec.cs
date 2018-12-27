// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        public Dictionary<string, Lazy<JValue>> InternalExtensionData { get; } = new Dictionary<string, Lazy<JValue>>();

        public bool ShouldSerializeInternalExtensionData() => false;

        public string GetName() => GetXrefPropertyValue("name");

        public string GetXrefPropertyValue(string property)
        {
            if (property is null)
                return null;

            if (ExtensionData.TryGetValue<JValue>(property, out var v))
            {
                return v.Value is string str ? str : null;
            }

            return InternalExtensionData.TryGetValue(property, out var internalValue) && internalValue.Value.Value is string internalStr ? internalStr : null;
        }

        public XrefSpec Clone()
        {
            var spec = new XrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };

            foreach (var (key, value) in ExtensionData)
            {
                spec.ExtensionData.TryAdd(key, value);
            }

            foreach (var (key, value) in InternalExtensionData)
            {
                spec.InternalExtensionData.TryAdd(key, value);
            }
            return spec;
        }
    }
}
