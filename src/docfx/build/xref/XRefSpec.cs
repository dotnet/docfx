// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class XrefSpec
    {
        public string Uid { get; set; }

        public string Href { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        public string GetName() => GetXrefPropertyValue("name");

        public string GetXrefPropertyValue(string property)
            => ExtensionData.TryGetValue(property, out var prop) && prop is JValue v && v.Value is string str ? str : null;
    }
}
