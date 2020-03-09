// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec : IXrefSpec
    {
        public string Uid { get; set; }

        public string Href { get; set; }

        public Document DeclaringFile { get; set; }

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JToken>> ExtensionData { get; } = new Dictionary<string, Lazy<JToken>>();

        public Dictionary<string, JsonSchemaContentType> PropertyContentTypeMapping { get; } = new Dictionary<string, JsonSchemaContentType>();

        public InternalXrefSpec(string uid, string href, Document declaringFile)
        {
            Uid = uid;
            Href = href;
            DeclaringFile = declaringFile;
        }

        public string? GetXrefPropertyValueAsString(string propertyName)
        {
            // for internal UID, the display property should only be plain text
            var contentType = GetXrefPropertyContentType(propertyName);
            if (contentType == JsonSchemaContentType.None)
            {
                return ExtensionData.TryGetValue(propertyName, out var property) && property.Value is JValue propertyValue && propertyValue.Value is string internalStr ? internalStr : null;
            }
            return null;
        }

        public string? GetName() => GetXrefPropertyValueAsString("name");

        public ExternalXrefSpec ToExternalXrefSpec()
        {
            var spec = new ExternalXrefSpec
            {
                Href = PathUtility.GetRelativePathToFile(DeclaringFile.SiteUrl, Href),
                Uid = Uid,
                Monikers = Monikers,
            };

            foreach (var (key, value) in ExtensionData)
            {
                spec.ExtensionData[key] = value.Value;
            }
            return spec;
        }

        private JsonSchemaContentType GetXrefPropertyContentType(string propertyName)
        {
            if (propertyName is null)
                return default;

            return PropertyContentTypeMapping.TryGetValue(propertyName, out var value) ? value : default;
        }
    }
}
