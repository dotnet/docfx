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

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JToken>> ExtensionData { get; } = new Dictionary<string, Lazy<JToken>>();

        public string GetXrefPropertyValue(string propertyName)
        {
            if (propertyName is null)
                return null;

            return ExtensionData.TryGetValue(propertyName, out var property) && property.Value is JValue propertyValue && propertyValue.Value is string internalStr ? internalStr : null;
        }

        public string GetName() => GetXrefPropertyValue("name");

        public ExternalXrefSpec ToExternalXrefSpec(Context context, Document file)
        {
            var spec = new ExternalXrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,

                // DHS appends branch infomation from cookie cache to URL, which is wrong for UID resolved URL
                // output xref map with URL appending "?branch=master" for master branch
                Href = file.Docset.Repository.Branch == "master" ? Href + "?branch=master" : Href,
            };
            foreach (var (key, value) in ExtensionData)
            {
                try
                {
                    spec.ExtensionData[key] = ExtensionData[key].Value;
                }
                catch (DocfxException ex)
                {
                    context.ErrorLog.Write(file.FilePath, ex.Error);
                }
            }
            return spec;
        }
    }
}
