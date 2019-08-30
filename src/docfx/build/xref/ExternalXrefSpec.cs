// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ExternalXrefSpec : IXrefSpec
    {
        public string Uid { get; set; }

        public string Href { get; set; }

        Document IXrefSpec.DeclaringFile => null;

        [JsonIgnore]
        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        public string GetName() => GetXrefPropertyValueAsString("name");

        public string GetXrefPropertyValueAsString(string propertyName)
        {
            if (propertyName != null && ExtensionData.TryGetValue<JValue>(propertyName, out var v))
            {
                return v.Value is string str ? str : null;
            }
            return null;
        }

        public ExternalXrefSpec ToExternalXrefSpec() => this;
    }
}
