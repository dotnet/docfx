// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ExternalXrefSpec : IXrefSpec
    {
        public string Uid { get; private set; } = "";

        public string? SchemaType { get; private set; }

        public string Href { get; private set; } = "";

        FilePath? IXrefSpec.DeclaringFile => null;

        [JsonIgnore]
        public MonikerList Monikers { get; private set; }

        [JsonIgnore]
        public string? RepositoryUrl { get; set; }

        [JsonIgnore]
        public string? DocsetName { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; private set; } = new JObject();

        public ExternalXrefSpec() { }

        public ExternalXrefSpec(string uid, string href, MonikerList monikers, string? schemaType)
        {
            Uid = uid;
            Href = href;
            Monikers = monikers;
            SchemaType = schemaType;
        }

        public string? GetXrefPropertyValueAsString(string propertyName)
        {
            if (ExtensionData.TryGetValue<JValue>(propertyName, out var v))
            {
                return v != null && v.Value is string str ? str : null;
            }
            return null;
        }

        public string? GetName() => GetXrefPropertyValueAsString("name");

        public ExternalXrefSpec ToExternalXrefSpec(string? overwriteHref = null) =>
            new ExternalXrefSpec(Uid, overwriteHref ?? Href, Monikers, SchemaType)
            {
                ExtensionData = ExtensionData,
            };
    }
}
