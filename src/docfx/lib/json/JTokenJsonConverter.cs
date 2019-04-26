// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// A JsonConverter that propagates line info to JToken.
    /// </summary>
    internal class JTokenJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JToken).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader is JTokenReader tokenReader)
            {
                var result = JsonUtility.DeepClone(tokenReader.CurrentToken);
                SkipToken(reader);
                return result;
            }
            return JToken.ReadFrom(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ((JToken)value).WriteTo(writer);
        }

        private static void SkipToken(JsonReader reader)
        {
            var currentDepth = reader.Depth;
            reader.Skip();
            while (reader.Depth > currentDepth)
            {
                if (!reader.Read())
                {
                    break;
                }
            }
        }
    }
}
