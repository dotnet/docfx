// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal record InternalXrefSpec(SourceInfo<string> Uid, string Href, FilePath DeclaringFile, MonikerList Monikers) : IXrefSpec
{
    public string? DeclaringPropertyPath { get; init; }

    public string? PropertyPath { get; init; }

    public bool UidGlobalUnique { get; init; }

    public string? SchemaType { get; init; }

    public Dictionary<string, Lazy<LocInfo<JToken>>> XrefProperties { get; } = new();

    string IXrefSpec.Uid => Uid.Value;

    public LocInfo<string?> GetXrefPropertyValue(string propertyName)
    {
        return
          XrefProperties.TryGetValue(propertyName, out var property)
          && property.Value.Value is JValue propertyValue
          && propertyValue.Value is string internalStr
          ? new LocInfo<string?>(internalStr, property.Value.Loc)
          : new LocInfo<string?>(default, new LocInfo(false));
    }

    public string? GetName() => GetXrefPropertyValue("name").Value;

    public LocInfo<string?> GetXrefPropertyOfName()
    {
        return GetXrefPropertyValue("name");
    }

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
            spec.ExtensionData[key] = value.Value.Value;
        }

        return spec;
    }
}
