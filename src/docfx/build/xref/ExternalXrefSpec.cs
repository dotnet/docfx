// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ExternalXrefSpec : IXrefSpec
    {
        private string? _name;

        public string Uid { get; set; } = "";

        public string Href { get; set; } = "";

        public string Name
        {
            get => _name ?? Uid;
            set => _name = value;
        }

        Document? IXrefSpec.DeclaringFile => null;

        [JsonIgnore]
        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        public string? GetXrefPropertyValueAsString(string propertyName)
        {
            if (ExtensionData.TryGetValue<JValue>(propertyName, out var v))
            {
                return v != null && v.Value is string str ? str : null;
            }
            return null;
        }

        public ExternalXrefSpec ToExternalXrefSpec() => this;
    }
}
