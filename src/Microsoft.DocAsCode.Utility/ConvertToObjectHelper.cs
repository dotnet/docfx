// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// The utility class for docascode project
/// </summary>
namespace Microsoft.DocAsCode.Utility
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json.Linq;

    public static class ConvertToObjectHelper
    {
        public static object ConvertExpandoObjectToObject(object raw)
        {
            if (raw is ExpandoObject)
            {
                return ((IDictionary<string, object>)raw).ToDictionary(s => s.Key, s => ConvertExpandoObjectToObject(s.Value));
            }
            if (raw is IEnumerable<object>)
            {
                return ((IEnumerable<object>)raw).Select(s => ConvertExpandoObjectToObject(s)).ToArray();
            }
            return raw;
        }

        public static object ConvertJObjectToObject(object raw)
        {
            var jValue = raw as JValue;
            if (jValue != null) { return jValue.Value; }
            var jArray = raw as JArray;
            if (jArray != null)
            {
                return jArray.Select(s => ConvertJObjectToObject(s)).ToArray();
            }
            var jObject = raw as JObject;
            if (jObject != null)
            {
                return jObject.ToObject<Dictionary<string, object>>().ToDictionary(p => p.Key, p => ConvertJObjectToObject(p.Value));
            }
            return raw;
        }

        public static object ConvertStrongTypeToObject(object raw)
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
                        return ConvertJObjectToObject(JsonUtility.Deserialize<object>(sr));
                    }
                }
            }
        }
    }
}
