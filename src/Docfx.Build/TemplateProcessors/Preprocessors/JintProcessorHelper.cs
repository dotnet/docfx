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
        else if (raw is IList<object> list)
        {
            var jsArray = new JsArray(engine, (uint)list.Count);
            foreach (var item in list)
            {
                jsArray.Push(ConvertObjectToJsValue(engine, item));
            }
            return jsArray;
        }
        else
        {
            return JsValue.FromObject(engine, raw);
        }
    }
}
