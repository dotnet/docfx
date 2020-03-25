// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec : IXrefSpec
    {
        private readonly Lazy<string?> _name;

        public SourceInfo<string> Uid { get; }

        public string Name => _name.Value ?? Uid;

        public string Href { get; set; }

        public Document DeclaringFile { get; }

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JToken>> XrefProperties { get; } = new Dictionary<string, Lazy<JToken>>();

        public Dictionary<string, JsonSchemaContentType> PropertyContentTypeMapping { get; } = new Dictionary<string, JsonSchemaContentType>();

        string IXrefSpec.Uid => Uid.Value;

        public InternalXrefSpec(SourceInfo<string> uid, Lazy<string?> name, string href, Document declaringFile)
        {
            Uid = uid;
            Href = href;
            DeclaringFile = declaringFile;
            _name = name;
        }

        public InternalXrefSpec(
            SourceInfo<string> uid,
            Lazy<string?> name,
            string href,
            Document declaringFile,
            Dictionary<string, Lazy<JToken>> xrefProperties)
        {
            Uid = uid;
            Href = href;
            DeclaringFile = declaringFile;
            _name = name;
            XrefProperties = xrefProperties;
        }

        public string? GetXrefPropertyValueAsString(string propertyName)
        {
            return XrefProperties.TryGetValue(propertyName, out var property) && property.Value is JValue propertyValue && propertyValue.Value is string internalStr ? internalStr : null;
        }

        public ExternalXrefSpec ToExternalXrefSpec()
        {
            var spec = new ExternalXrefSpec
            {
                Href = PathUtility.GetRelativePathToFile(DeclaringFile.SiteUrl, Href),
                Uid = Uid,
                Monikers = Monikers,
                Name = Name,
            };

            foreach (var (key, value) in XrefProperties)
            {
                spec.ExtensionData[key] = value.Value;
            }
            return spec;
        }
    }
}
