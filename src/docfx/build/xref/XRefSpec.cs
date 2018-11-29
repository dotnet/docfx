// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

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
        public Dictionary<string, object> ExtensionData { get; } = new Dictionary<string, object>();

        public string GetName() => GetXrefPropertyValue("name");

        public string GetXrefPropertyValue(string property)
        {
            if (!string.IsNullOrEmpty(property) && ExtensionData.TryGetValue(property, out var v) && v != null)
                return v.ToString();
            return null;
        }
    }
}
