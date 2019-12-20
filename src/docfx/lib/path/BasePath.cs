// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(BasePathJsonConverter))]
    internal readonly struct BasePath
    {
        private readonly string _original;

        private readonly string _relativePath;

        public string Original => _original ?? "";

        // It is either an empty string, or a path without leading /
        public string RelativePath => _relativePath ?? "";

        public BasePath(string value)
        {
            _original = value;
            _relativePath = value.StartsWith('/') ? value.TrimStart('/') : value;
        }

        public override string ToString() => _original;

        private class BasePathJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(PathString);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var value = serializer.Deserialize<string>(reader);
                return new BasePath(value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                => throw new NotSupportedException();
        }
    }
}
