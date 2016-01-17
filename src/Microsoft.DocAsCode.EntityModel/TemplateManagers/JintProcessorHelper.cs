// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.IO;
    using System.Threading;

    using Jint;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JintProcessorHelper
    {
        private static readonly ThreadLocal<JsonSerializer> _toJsValueSerializer = new ThreadLocal<JsonSerializer>(
            () =>
            {
                var jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                jsonSerializer.Converters.Add(new JObjectToJsValueConverter());
                return jsonSerializer;
            });

        public static Jint.Native.JsValue ConvertStrongTypeToJsValue(object raw)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    JsonUtility.Serialize(sw, raw);
                    sw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        return JsonUtility.Deserialize<Jint.Native.JsValue>(sr, _toJsValueSerializer.Value);
                    }
                }
            }
        }

        private sealed class JObjectToJsValueConverter : JsonConverter
        {
            private static readonly Engine DefaultEngine = new Engine();
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Jint.Native.JsValue);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    var jArray = JArray.Load(reader);
                    return ConvertJObjectToJsValue(jArray);
                }
                else
                {
                    var jObject = JObject.Load(reader);
                    var converted = ConvertJObjectToJsValue(jObject);
                    return converted;
                }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            private static Jint.Native.JsValue ConvertJObjectToJsValue(object raw)
            {
                var jArray = raw as JArray;
                if (jArray != null)
                {
                    var jsArray = DefaultEngine.Array.Construct(Jint.Runtime.Arguments.Empty);
                    foreach (var item in jArray)
                    {
                        DefaultEngine.Array.PrototypeObject.Push(jsArray, Jint.Runtime.Arguments.From(ConvertJObjectToJsValue(item)));
                    }
                    return jsArray;
                }
                var jObject = raw as JObject;
                if (jObject != null)
                {
                    var jsObject = DefaultEngine.Object.Construct(Jint.Runtime.Arguments.Empty);
                    foreach (var pair in jObject)
                    {
                        jsObject.Put(pair.Key, ConvertJObjectToJsValue(pair.Value), true);
                    }
                    return jsObject;
                }

                var jValue = raw as JValue;
                if (jValue != null)
                {
                    return Jint.Native.JsValue.FromObject(DefaultEngine, jValue.Value);
                }

                return Jint.Native.JsValue.FromObject(DefaultEngine, raw);
            }
        }
    }
}
