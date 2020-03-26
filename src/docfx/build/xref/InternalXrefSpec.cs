// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec : IXrefSpec
    {
        public SourceInfo<string> Uid { get; }

        public string Name => GetXrefPropertyValueAsString("name") ?? Uid;

        public string Href { get; set; }

        public Document DeclaringFile { get; }

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JToken>> XrefProperties { get; } = new Dictionary<string, Lazy<JToken>>();

        string IXrefSpec.Uid => Uid.Value;

        public InternalXrefSpec(SourceInfo<string> uid, string href, Document declaringFile)
        {
            Uid = uid;
            Href = href;
            DeclaringFile = declaringFile;
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
