// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec
    {
        public string Uid { get; set; }

        public string Href { get; set; }

        public Document ReferencedFile { get; set; }

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<(List<Error> errors, JValue jValue)>> ExtensionData { get; } = new Dictionary<string, Lazy<(List<Error> errors, JValue jValue)>>();

        public (List<Error> errors, string value) GetXrefPropertyValue(string propertyName)
        {
            var errors = new List<Error>();

            if (propertyName is null)
                return (errors, null);

            if (ExtensionData.TryGetValue(propertyName, out var internalValue))
            {
                var jValue = errors.AddRange(internalValue.Value);
                if (jValue.Value is string value)
                {
                    return (errors, value);
                }
            }

            return (errors, null);
        }

        public (List<Error> errors, string value) GetName() => GetXrefPropertyValue("name");

        public (List<Error> errors, XrefSpec xrefSpec) ToExternalXrefSpec(Context context, Document file)
        {
            var errors = new List<Error>();
            var spec = new XrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };
            foreach (var (key, value) in ExtensionData)
            {
                try
                {
                    spec.ExtensionData[key] = errors.AddRange(GetXrefPropertyValue(key));
                }
                catch (DocfxException ex)
                {
                    errors.Add(ex.Error);
                }
            }
            return (errors, spec);
        }
    }
}
