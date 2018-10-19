// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// The utility class for docascode project
/// </summary>
namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    public static class ConvertToObjectHelper
    {
        public static object ConvertExpandoObjectToObject(object raw)
        {
            return ConvertExpandoObjectToObjectCore(raw, new Dictionary<object, object>());
        }

        public static object ConvertJObjectToObject(object raw)
        {
            if (raw is JValue jValue)
            {
                return jValue.Value;
            }
            if (raw is JArray jArray)
            {
                return jArray.Select(s => ConvertJObjectToObject(s)).ToArray();
            }
            if (raw is JObject jObject)
            {
                return jObject.ToObject<Dictionary<string, object>>().ToDictionary(p => p.Key, p => ConvertJObjectToObject(p.Value));
            }
            return raw;
        }

        public static object ConvertStrongTypeToObject(object raw)
        {
            return ConvertJObjectToObject(ConvertStrongTypeToJObject(raw));
        }

        public static object ConvertStrongTypeToJObject(object raw)
        {
            if (raw is JToken)
            {
                return raw;
            }

            return JToken.FromObject(raw, JsonUtility.DefaultSerializer.Value);
        }

        public static object ConvertToDynamic(object obj)
        {
            return ConvertToDynamicCore(obj, new Dictionary<object, object>());
        }

        private static object ConvertExpandoObjectToObjectCore(object obj, Dictionary<object, object> cache)
        {
            if (obj == null)
            {
                return null;
            }

            if (cache.TryGetValue(obj, out var output))
            {
                return output;
            }

            var result = obj;

            if (obj is ExpandoObject eo)
            {
                result = cache[obj] = new Dictionary<string, object>();
                foreach(var pair in eo)
                {
                    ((Dictionary<string, object>)result)[pair.Key] = ConvertExpandoObjectToObjectCore(pair.Value, cache);
                }
            }
            else if (obj is IEnumerable<object> enumerable)
            {
                result = cache[obj] = new List<object>();
                foreach (var item in enumerable)
                {
                    ((List<object>)result).Add(ConvertExpandoObjectToObjectCore(item, cache));
                }
            }

            return result;
        }

        private static object ConvertToDynamicCore(object obj, Dictionary<object, object> cache)
        {
            if (obj == null)
            {
                return null;
            }

            if (cache.TryGetValue(obj, out var output))
            {
                return output;
            }
            var result = obj;
            if (obj is ExpandoObject)
            {
                result = cache[obj] = obj;
            }
            else if (obj is IDictionary<object, object> dict)
            {
                result = cache[obj] = new ExpandoObject();

                foreach (var pair in dict)
                {
                    if (!(pair.Key is string key))
                    {
                        throw new NotSupportedException("Only string key is supported.");
                    }

                    ((IDictionary<string, Object>)result).Add(key, ConvertToDynamicCore(pair.Value, cache));
                }
            }
            else if (obj is IDictionary<string, object> sdict)
            {
                result = cache[obj] = new ExpandoObject();

                foreach (var pair in sdict)
                {
                    ((IDictionary<string, Object>)result).Add(pair.Key, ConvertToDynamicCore(pair.Value, cache));
                }
            }
            else if (obj is IList<object> array)
            {
                result = cache[obj] = array;
                for (int i = 0; i < array.Count; i++)
                {
                    ((IList<object>)result)[i] = ConvertToDynamicCore(array[i], cache);
                }
            }

            return result;
        }
    }
}
