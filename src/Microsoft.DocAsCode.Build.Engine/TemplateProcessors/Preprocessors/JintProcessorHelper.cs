// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Jint;

    public static class JintProcessorHelper
    {
        private static readonly Engine DefaultEngine = new Engine();

        public static Jint.Native.JsValue ConvertObjectToJsValue(object raw)
        {
            if (raw is IDictionary<string, object> idict)
            {
                var jsObject = DefaultEngine.Object.Construct(Jint.Runtime.Arguments.Empty);
                foreach (var pair in idict)
                {
                    jsObject.Put(pair.Key, ConvertObjectToJsValue(pair.Value), true);
                }
                return jsObject;
            }
            else if (raw is IList<object> list)
            {
                var jsArray = DefaultEngine.Array.Construct(Jint.Runtime.Arguments.Empty);
                foreach (var item in list)
                {
                    DefaultEngine.Array.PrototypeObject.Push(jsArray, Jint.Runtime.Arguments.From(ConvertObjectToJsValue(item)));
                }
                return jsArray;
            }
            else
            {
                return Jint.Native.JsValue.FromObject(DefaultEngine, raw);
            }
        }
    }
}
