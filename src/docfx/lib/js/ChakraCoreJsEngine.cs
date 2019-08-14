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
        private static readonly JavaScriptPropertyId s_lengthProperty = JavaScriptPropertyId.FromString("length");
        private static readonly JavaScriptPropertyId s_globalProperty = JavaScriptPropertyId.FromString("__global");

        private static readonly JavaScriptNativeFunction s_requireFunction = new JavaScriptNativeFunction(Require);

        private static readonly ThreadLocal<Stack<string>> t_dirnames = new ThreadLocal<Stack<string>>(() => new Stack<string>());

        private static readonly ThreadLocal<Dictionary<string, JavaScriptValue>> t_modules = new ThreadLocal<Dictionary<string, JavaScriptValue>>();

        private static JavaScriptSourceContext s_currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);

        private readonly string _scriptDir;
        private readonly JObject _global;

        public ChakraCoreJsEngine(string scriptDir, JObject global = null)
        {
            _scriptDir = scriptDir;
            _global = global;
        }

        public JToken Run(string scriptPath, string methodName, JToken arg)
        {
            var runtime = JavaScriptRuntime.Create();
            var context = runtime.CreateContext();

            using (new JavaScriptContext.Scope(context))
            {
                try
                {
                    t_modules.Value = new Dictionary<string, JavaScriptValue>(PathUtility.PathComparer);

                    var exports = Run(Path.GetFullPath(Path.Combine(_scriptDir, scriptPath)));
                    var method = exports.GetProperty(JavaScriptPropertyId.FromString(methodName));
                    var input = ToJavaScriptValue(arg);

                    input.SetProperty(s_globalProperty, ToJavaScriptValue(_global), useStrictRules: true);

                    var output = method.CallFunction(JavaScriptValue.Undefined, input);

                    return ToJToken(output);
                }
                catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
                {
                    throw new JavaScriptScriptException(ex.ErrorCode, ex.Error, $"{ex.ErrorCode}:\n{ToJToken(ex.Error)}");
                }
                finally
                {
                    t_modules.Value = null;
                }
            }
        }

        private static JavaScriptValue Run(string scriptPath)
        {
            var modules = t_modules.Value;
            if (modules.TryGetValue(scriptPath, out var module))
            {
                return module;
            }

            var exports = modules[scriptPath] = JavaScriptValue.CreateObject();
            var sourceCode = File.ReadAllText(scriptPath);

            // add `process` to input to get the correct file path while running script inside docs-ui
            var script = $@"(function (module, exports, __dirname, __global, require, process) {{{sourceCode}}})";
            var dirname = Path.GetDirectoryName(scriptPath);

            t_dirnames.Value.Push(dirname);

            try
            {
                JavaScriptContext.RunScript(script, s_currentSourceContext++, scriptPath).CallFunction(
                    JavaScriptValue.Undefined, // this pointer
                    JavaScriptValue.CreateObject(),
                    exports,
                    JavaScriptValue.FromString(dirname),
                    JavaScriptValue.CreateFunction(s_requireFunction),
                    JavaScriptValue.CreateObject());

                return exports;
            }
            finally
            {
                t_dirnames.Value.Pop();
            }
        }

        private static JavaScriptValue Require(
            JavaScriptValue callee,
            [MarshalAs(UnmanagedType.U1)] bool isConstructCall,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] JavaScriptValue[] arguments,
            ushort argumentCount,
            IntPtr callbackData)
        {
            // First argument is this pointer
            if (argumentCount < 2)
            {
                return JavaScriptValue.CreateObject();
            }

            try
            {
                var dirname = t_dirnames.Value.Peek();
                var scriptPath = Path.GetFullPath(Path.Combine(dirname, arguments[1].ToString()));

                return Run(scriptPath);
            }
            catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
            {
                JavaScriptContext.SetException(ex.Error);
                return JavaScriptValue.CreateObject();
            }
            catch (Exception ex)
            {
                JavaScriptContext.SetException(JavaScriptValue.CreateError(JavaScriptValue.FromString(ex.Message)));
                return JavaScriptValue.CreateObject();
            }
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

                case JavaScriptValueType.Undefined:
                    return JValue.CreateUndefined();

                case JavaScriptValueType.String:
                    return value.ToString();

                case JavaScriptValueType.Number:
                    var intNumber = value.ToInt32();
                    var doubleNumber = value.ToDouble();
                    return intNumber == doubleNumber ? (JValue)intNumber : (JValue)doubleNumber;

                case JavaScriptValueType.Array:
                    var arr = new JArray();
                    var arrLength = value.GetProperty(s_lengthProperty).ToInt32();
                    for (var i = 0; i < arrLength; i++)
                    {
                        arr.Add(ToJToken(value.GetIndexedProperty(JavaScriptValue.FromInt32(i))));
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
