// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class ManifestItemCollectionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ManifestItemCollection);
    }

    public override object ReadJson(JsonReader reader, Type objecType, object existingValue,
        JsonSerializer serializer)
    {
        var manifestCollectionList = (List<ManifestItem>) serializer.Deserialize(reader, typeof(List<ManifestItem>));
        if (existingValue != null)
        {
            ((ManifestItemCollection)existingValue).AddRange(manifestCollectionList);
            return existingValue;
        }
        return new ManifestItemCollection(manifestCollectionList);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var sortedManifestFiles = ((ManifestItemCollection)value).OrderBy(
            obj => obj.SourceRelativePath ?? string.Empty,
            StringComparer.Ordinal).ToList();

        serializer.Serialize(writer, sortedManifestFiles);
    }
}
