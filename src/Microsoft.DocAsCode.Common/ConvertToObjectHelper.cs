// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// The utility class for docascode project
/// </summary>
namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    public static class ConvertToObjectHelper
    {
        public static object ConvertExpandoObjectToObject(object raw)
        {
            if (raw is ExpandoObject)
            {
                return ((IDictionary<string, object>)raw).ToDictionary(s => s.Key, s => ConvertExpandoObjectToObject(s.Value));
            }
            if (raw is IEnumerable<object> enumerable)
            {
                return enumerable.Select(s => ConvertExpandoObjectToObject(s)).ToArray();
            }
            return raw;
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
    }
}
