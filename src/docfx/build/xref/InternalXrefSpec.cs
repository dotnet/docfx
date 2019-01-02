// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec : XrefSpec
    {
        public Dictionary<string, Lazy<Func<string, string, Document, Document, JValue>>> InternalExtensionData { get; } = new Dictionary<string, Lazy<Func<string, string, Document, Document, JValue>>>();

        public string GetXrefPropertyValue(string property, string uid, Document referencedFile, Document rootFile)
        {
            if (property is null)
                return null;

            return InternalExtensionData.TryGetValue(property, out var internalValue) && internalValue.Value(property, uid, referencedFile, rootFile).Value is string internalStr ? internalStr : null;
        }

        public string GetName(Document referencedFile, Document rootFile) => GetXrefPropertyValue("name", Uid, referencedFile, rootFile);

        public XrefSpec ToExternalXrefSpec(Document referencedFile, Document rootFile)
        {
            var spec = new XrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };
            foreach (var (key, value) in InternalExtensionData)
            {
                spec.ExtensionData[key] = value.Value(key, Uid, referencedFile, rootFile);
            }
            return spec;
        }

        public new InternalXrefSpec Clone()
        {
            var spec = new InternalXrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };

            foreach (var (key, value) in InternalExtensionData)
            {
                spec.InternalExtensionData[key] = value;
            }
            return spec;
        }
    }
}
