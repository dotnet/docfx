// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    public class ManifestItemCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ManifestItemCollection);
        }

        public override object ReadJson(JsonReader reader, Type objecType, object existingValue,
            JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, objecType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var sortedManifestFiles = ((ManifestItemCollection) value).OrderBy(
                obj => obj.SourceRelativePath ?? String.Empty,
                StringComparer.Ordinal).ToList();

            serializer.Serialize(writer, sortedManifestFiles);
        }
    }
}
