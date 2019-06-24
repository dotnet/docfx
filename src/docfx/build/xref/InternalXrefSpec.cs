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

        public Document DeclairingFile { get; set; }

        public string[] Monikers { get; set; } = Array.Empty<string>();

        public Dictionary<string, Lazy<JToken>> ExtensionData { get; } = new Dictionary<string, Lazy<JToken>>();

        public string GetXrefPropertyValue(string propertyName)
        {
            if (propertyName is null)
                return null;

            return ExtensionData.TryGetValue(propertyName, out var property) && property.Value is JValue propertyValue && propertyValue.Value is string internalStr ? internalStr : null;
        }

        public string GetName() => GetXrefPropertyValue("name");

        public ExternalXrefSpec ToExternalXrefSpec(Context context)
        {
            var spec = new ExternalXrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };
            foreach (var (key, value) in ExtensionData)
            {
                try
                {
                    spec.ExtensionData[key] = ExtensionData[key].Value;
                }
                catch (DocfxException ex)
                {
                    context.ErrorLog.Write(DeclairingFile.FilePath, ex.Error);
                }
            }
            return spec;
        }
    }
}
