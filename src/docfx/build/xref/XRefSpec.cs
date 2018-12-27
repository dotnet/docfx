// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class XrefSpec
    {
        public string Uid { get; set; }

        // not into output for now
        [JsonIgnore]
        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public string Href { get; set; }

        [JsonExtensionData]
        public Dictionary<string, Lazy<(List<Error>, object)>> ExtensionData { get; set; }

        public string GetName() => GetXrefPropertyValue("name");

        public string GetXrefPropertyValue(string property, Report report)
        {
            if (ExtensionData.TryGetValue(property, out var v))
            {
                var (errors, result) = v.Value;
                report.Write(errors);
            }
        }

        public XrefSpec Clone()
        {
            var spec = new XrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };

            foreach (var (key, value) in ExtensionData)
            {
                spec.ExtensionData.TryAdd(key, value);
            }
            return spec;
        }
    }
}
