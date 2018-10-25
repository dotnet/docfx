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
        {
            var obj = GetXrefProperty(property);
            return obj != null && obj.TryGetValue("value", out var val) && val is JValue v && v.Value is string str ? str : null;
        }

        public bool IsXrefPropertyHtml(string property)
        {
            var obj = GetXrefProperty(property);
            return obj != null && obj.TryGetValue("isHtml", out var val) && val is JValue v && v.Value is bool b ? b : false;
        }

        private JObject GetXrefProperty(string property) => ExtensionData.TryGetValue(property, out var val) && val is JObject obj ? obj : null;
    }
}
