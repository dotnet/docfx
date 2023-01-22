// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Jint;
    using Jint.Native;

    public static class JintProcessorHelper
    {
        private static readonly Engine DefaultEngine = new Engine();

        public static JsValue ConvertObjectToJsValue(object raw)
        {
            if (raw is IDictionary<string, object> idict)
            {
                var jsObject = new JsObject(DefaultEngine);
                foreach (var pair in idict)
                {
                    jsObject.FastSetDataProperty(pair.Key, ConvertObjectToJsValue(pair.Value));
                }
                return jsObject;
            }
            else if (raw is IList<object> list)
            {
                var jsArray = new JsArray(DefaultEngine, (uint) list.Count);
                foreach (var item in list)
                {
                    jsArray.Push(ConvertObjectToJsValue(item));
                }
                return jsArray;
            }
            else
            {
                return JsValue.FromObject(DefaultEngine, raw);
            }
        }
    }
}
