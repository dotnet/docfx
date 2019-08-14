// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ChakraHost.Hosting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Javascript engine based on https://github.com/microsoft/ChakraCore
    /// </summary>
    internal class ChakraCoreJsEngine : IJavascriptEngine
    {
        private static readonly ThreadLocal<JavaScriptRuntime> t_runtimes = new ThreadLocal<JavaScriptRuntime>(
            () =>
            {
                return JavaScriptRuntime.Create();
            });

        private static readonly JavaScriptPropertyId s_lengthProperty = JavaScriptPropertyId.FromString("length");

        private static JavaScriptSourceContext s_currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);

        private readonly string _scriptDir;

        public ChakraCoreJsEngine(string scriptDir, JObject global = null)
        {
            _scriptDir = scriptDir;
        }

        public JToken Run(string scriptPath, string methodName, JToken arg)
        {
            var runtime = JavaScriptRuntime.Create();

            var context = runtime.CreateContext();

            using (new JavaScriptContext.Scope(context))
            {
                var sourceName = Path.GetFullPath(Path.Combine(_scriptDir, scriptPath));
                var script = File.ReadAllText(sourceName);

                try
                {
                    return ToJToken(JavaScriptContext.RunScript(script, s_currentSourceContext++, sourceName));
                }
                catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
                {
                    throw new JavaScriptScriptException(ex.ErrorCode, ex.Error, $"Javascript error:\n{ToJToken(ex.Error)}");
                }
            }
        }

        public JToken Run(string scriptPath, string methodName, JToken arg)
        {
            return default;
        }

        public JavaScriptValue Run(string scriptPath)
        {
            var runtime = JavaScriptRuntime.Create();

            var context = runtime.CreateContext();

            var modules = new Dictionary<string, JavaScriptValue>();

            return RunCore(scriptPath);

            JavaScriptValue RunCore(string path)
            {
                var fullPath = Path.GetFullPath(path);
                if (modules.TryGetValue(fullPath, out var module))
                {
                    return module;
                }

                var exports = modules[fullPath] = JavaScriptValue.CreateObject();
                var sourceCode = File.ReadAllText(fullPath);

                // add process to input to get the correct file path while running script inside docs-ui
                var script = $@"
;(function (module, exports, __dirname, require, process) {{
{sourceCode}
}})
";
                var dirname = Path.GetDirectoryName(fullPath);
                var require = JavaScriptValue.CreateFunction(Require);

                try
                {
                    return JavaScriptContext.RunScript(script, s_currentSourceContext++, fullPath).CallFunction(
                        JavaScriptValue.CreateObject(),
                        exports,
                        JavaScriptValue.FromString(dirname),
                        require,
                        JavaScriptValue.CreateObject());
                }
                catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
                {
                    throw new JavaScriptScriptException(ex.ErrorCode, ex.Error, $"Javascript error:\n{ToJToken(ex.Error)}");
                }
            }
        }

        private static JavaScriptValue Require(
            JavaScriptValue callee,
            [MarshalAs(UnmanagedType.U1)] bool isConstructCall,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] JavaScriptValue[] arguments,
            ushort argumentCount,
            IntPtr callbackData)
        {
            return default;
        }

        private static JavaScriptValue ToJavaScriptValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Null:
                    return JavaScriptValue.Null;

                case JTokenType.Undefined:
                    return JavaScriptValue.Undefined;

                case JTokenType.Boolean:
                    return JavaScriptValue.FromBoolean(token.Value<bool>());

                case JTokenType.String:
                    return JavaScriptValue.FromString(token.Value<string>());

                case JTokenType.Integer:
                    return JavaScriptValue.FromInt32(token.Value<int>());

                case JTokenType.Float:
                    return JavaScriptValue.FromDouble(token.Value<double>());

                case JTokenType.Array when token is JArray arr:
                    var resultArray = JavaScriptValue.CreateArray((uint)arr.Count);
                    for (var i = 0; i < arr.Count; i++)
                    {
                        resultArray.SetIndexedProperty(
                            JavaScriptValue.FromInt32(i), ToJavaScriptValue(arr[i]));
                    }
                    return resultArray;

                case JTokenType.Object when token is JObject obj:
                    var resultObj = JavaScriptValue.CreateObject();
                    foreach (var (name, value) in obj)
                    {
                        resultObj.SetProperty(
                            JavaScriptPropertyId.FromString(name), ToJavaScriptValue(value), useStrictRules: true);
                    }
                    return resultObj;

                default:
                    throw new NotSupportedException($"Cannot marshal JToken type '{token.Type}'");
            }
        }

        private static JToken ToJToken(JavaScriptValue value)
        {
            switch (value.ValueType)
            {
                case JavaScriptValueType.Boolean:
                    return value.ToBoolean();

                case JavaScriptValueType.Null:
                    return JValue.CreateNull();

                case JavaScriptValueType.Number:
                    return value.ToDouble();

                case JavaScriptValueType.String:
                    return value.ToString();

                case JavaScriptValueType.Undefined:
                    return JValue.CreateUndefined();

                case JavaScriptValueType.Array:
                    var arr = new JArray();
                    var arrLength = value.GetProperty(s_lengthProperty).ToInt32();
                    for (var i = 0; i < arrLength; i++)
                    {
                        arr[i] = ToJToken(value.GetIndexedProperty(JavaScriptValue.FromInt32(i)));
                    }
                    return arr;

                case JavaScriptValueType.Object:
                case JavaScriptValueType.Error:
                    var obj = new JObject();
                    var names = value.GetOwnPropertyNames();
                    var namesLength = names.GetProperty(s_lengthProperty).ToInt32();
                    for (var i = 0; i < namesLength; i++)
                    {
                        var name = names.GetIndexedProperty(JavaScriptValue.FromInt32(i)).ToString();
                        obj[name] = ToJToken(value.GetProperty(JavaScriptPropertyId.FromString(name)));
                    }
                    return obj;

                default:
                    throw new NotSupportedException($"Cannot marshal javascript type '{value.ValueType}'");
            }
        }
    }
}
