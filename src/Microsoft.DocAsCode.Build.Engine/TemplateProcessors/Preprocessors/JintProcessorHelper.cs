// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Jint.Native;

namespace Microsoft.DocAsCode.Build.Engine;

public static class JintProcessorHelper
{
    public static JsValue ConvertObjectToJsValue(Jint.Engine engine, object raw)
    {
        if (raw is IDictionary<string, object> idict)
        {
            var jsObject = new JsObject(engine);
            foreach (var pair in idict)
            {
                jsObject.FastSetDataProperty(pair.Key, ConvertObjectToJsValue(engine, pair.Value));
            }
            return jsObject;
        }
        else if (raw is IList<object> list)
        {
            var jsArray = new JsArray(engine, (uint) list.Count);
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
