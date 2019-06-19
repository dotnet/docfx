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

        public JToken GetXrefProperty(string propertyName) => ExtensionData[propertyName]?.Value;

        public ExternalXrefSpec ToExternalXrefSpec()
        {
            var spec = new ExternalXrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };

            foreach (var (key, value) in ExtensionData)
            {
                spec.ExtensionData[key] = value.Value;
            }

            return spec;
        }
    }
}
