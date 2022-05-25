// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal record ExternalXrefSpec : IXrefSpec
{
    public string Uid { get; init; } = "";

    public string Href { get; init; } = "";

    public string? SchemaType { get; init; }

    [JsonIgnore]
    public MonikerList Monikers { get; init; }

    public string? MonikerGroup => Monikers.MonikerGroup;

    [JsonIgnore]
    public string? RepositoryUrl { get; set; }

    [JsonIgnore]
    public string? DocsetName { get; set; }

    [JsonExtensionData]
    public JObject ExtensionData { get; init; } = new JObject();

    FilePath? IXrefSpec.DeclaringFile => null;

    public string? GetXrefPropertyValueAsString(string propertyName)
    {
        if (ExtensionData.TryGetValue<JValue>(propertyName, out var v))
        {
            return v != null && v.Value is string str ? str : null;
        }
        return null;
    }

    public string? GetName() => GetXrefPropertyValueAsString("name");

    public ExternalXrefSpec ToExternalXrefSpec(string? overwriteHref = null, MonikerList? monikerList = null)
    {
        return this with
        {
            Href = overwriteHref ?? Href,
            Monikers = monikerList ?? Monikers,
        };
    }
}
