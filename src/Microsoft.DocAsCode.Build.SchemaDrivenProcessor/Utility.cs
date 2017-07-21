// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDrivenProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;

    public static class DynamicConverter
    {
        public static object Convert(object obj)
        {
            var dict = obj as Dictionary<object, object>;
            if (dict != null)
            {
                return DictionaryToDynamic(dict);
            }

            var array = obj as List<object>;
            if (array != null)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    array[i] = Convert(array[i]);
                }
            }
            return obj;
        }

        private static object DictionaryToDynamic(Dictionary<object, object> value)
        {
            var obj = new ExpandoObject();
            foreach (var pair in value)
            {
                var key = pair.Key as string;
                if (key == null)
                {
                    throw new NotSupportedException("Only string key is supported.");
                }
                ((IDictionary<String, Object>)obj).Add(key, Convert(pair.Value));
            }

            return obj;
        }
    }
}
