// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal record InternalXrefSpec(SourceInfo<string> Uid, string Href, FilePath DeclaringFile, MonikerList Monikers) : IXrefSpec
{
    public string? PropertyPath { get; init; }

    public bool UidGlobalUnique { get; init; }

    public string? SchemaType { get; init; }

    public bool IsNameLocalizable { get; set; }

    public Dictionary<string, Lazy<JToken>> XrefProperties { get; } = new();

    string IXrefSpec.Uid => Uid.Value;

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
        var spec = new ExternalXrefSpec
        {
            Uid = Uid,
            Href = overwriteHref ?? Href,
            Monikers = Monikers,
            SchemaType = SchemaType,
        };

        foreach (var (key, value) in XrefProperties)
        {
            spec.ExtensionData[key] = value.Value;
        }

        return spec;
    }
}
