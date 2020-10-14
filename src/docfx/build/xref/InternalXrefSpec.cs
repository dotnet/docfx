// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec : IXrefSpec
    {
        public SourceInfo<string> Uid { get; }

        public string? SchemaType { get; }

        public string Href { get; }

        public FilePath DeclaringFile { get; }

        public MonikerList Monikers { get; }

        public Dictionary<string, Lazy<JToken>> XrefProperties { get; } = new Dictionary<string, Lazy<JToken>>();

        string IXrefSpec.Uid => Uid.Value;

        [JsonIgnore]
        public bool UIDGlobalUnique { get; set; }

        // TODO: change to use xrefSpec type to express what kind of xref spec it is: e.g. achievement, module
        [JsonIgnore]
        internal string? DeclaringPropertyPath { get; }

        public InternalXrefSpec(
            SourceInfo<string> uid,
            string href,
            FilePath declaringFile,
            MonikerList monikerList,
            string? declaringPropertyPath = null,
            string? schemaType = null)
        {
            Uid = uid;
            Href = href;
            DeclaringFile = declaringFile;
            Monikers = monikerList;
            DeclaringPropertyPath = declaringPropertyPath;
            SchemaType = schemaType;
        }

        public string? GetXrefPropertyValueAsString(string propertyName)
        {
            return
              XrefProperties.TryGetValue(propertyName, out var property) && property.Value is JValue propertyValue && propertyValue.Value is string internalStr
              ? internalStr
              : null;
        }

        public string? GetName() => GetXrefPropertyValueAsString("name");

        public ExternalXrefSpec ToExternalXrefSpec(string? overwriteHref = null)
        {
            var spec = new ExternalXrefSpec(Uid, overwriteHref ?? Href, Monikers, SchemaType);

            foreach (var (key, value) in XrefProperties)
            {
                spec.ExtensionData[key] = value.Value;
            }
            return spec;
        }
    }
}
