// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(BasePathJsonConverter))]
    internal class BasePath
    {
        public string Original { get; set; }

        private string RelativePath
            => Original.StartsWith('/') ? Original.TrimStart('/') : Original;

        public BasePath(string value)
        {
            Original = value;
        }

        public static implicit operator string(in BasePath basePath) => basePath.RelativePath;

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
