// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class ValueTupleConverter<T1, T2> : JsonConverter
    {
        private readonly string _prop1;
        private readonly string _prop2;

        public ValueTupleConverter(string prop1, string prop2)
        {
            _prop1 = prop1 ?? throw new ArgumentNullException(nameof(prop1));
            _prop2 = prop2 ?? throw new ArgumentNullException(nameof(prop2));
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new InvalidOperationException();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ValueTuple<T1, T2>) == objectType;
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var result = new List<(T1, T2)>();

            if (reader.TokenType == JsonToken.StartArray)
            {
                var jArray = JArray.Load(reader);
                foreach (var item in jArray)
                {
                    var obj = JObject.FromObject(item);
                    var properties = obj.Properties().ToList();
                    if (properties.Any(p => p.Name.Equals(_prop1, StringComparison.OrdinalIgnoreCase))
                    && properties.Any(p => p.Name.Equals(_prop2, StringComparison.OrdinalIgnoreCase))
                    && obj[_prop1] != null
                    && obj[_prop2] != null)
                    {
                        result.Add((obj[_prop1!]!.ToObject<T1>(), obj[_prop2!]!.ToObject<T2>()));
                    }
                }
            }
            return result.ToArray();
        }
    }
}
