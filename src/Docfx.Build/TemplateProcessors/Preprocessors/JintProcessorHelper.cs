// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Jint.Native;

namespace Docfx.Build.Engine;

public static class JintProcessorHelper
{
    public static JsValue ConvertObjectToJsValue(Jint.Engine engine, object raw)
    {
        if (raw is IDictionary<string, object> dict)
        {
            var jsObject = new JsObject(engine);
            foreach (var pair in dict)
            {
                jsObject.FastSetDataProperty(pair.Key, ConvertObjectToJsValue(engine, pair.Value));
            }
            return jsObject;
        }

        if (raw is IList<object> list)
        {
            // allow Jint to take ownership of the array
            var elements = new JsValue[list.Count];
            for (int i = 0; i < (uint)elements.Length; i++)
            {
                elements[i] = ConvertObjectToJsValue(engine, list[i]);
            }

            return new JsArray(engine, elements);
        }

        return JsValue.FromObject(engine, raw);
    }
}
