// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
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
    internal class ChakraCoreJsEngine : IJavaScriptEngine
    {
        private static readonly JavaScriptNativeFunction s_requireFunction = new JavaScriptNativeFunction(Require);
        private static readonly ThreadLocal<Stack<string>> t_dirnames = new ThreadLocal<Stack<string>>(() => new Stack<string>());
        private static readonly ThreadLocal<Dictionary<string, JavaScriptValue>?> t_modules = new ThreadLocal<Dictionary<string, JavaScriptValue>?>();

        private static int s_currentSourceContext;

        private readonly JavaScriptContext _context = CreateContext();
        private readonly JavaScriptValue _global;
        private readonly string _scriptDir;

        private readonly Dictionary<string, JavaScriptValue> _scriptExports = new Dictionary<string, JavaScriptValue>();

        public ChakraCoreJsEngine(string scriptDir, JObject? global = null)
        {
            _scriptDir = scriptDir;

            if (global != null)
            {
                _global = RunInContext(() => ToJavaScriptValue(global));
                _global.AddRef();
            }
        }

        public JToken Run(string scriptPath, string methodName, JToken arg)
        {
            return RunInContext(() =>
            {
                var exports = GetScriptExports(Path.GetFullPath(Path.Combine(_scriptDir, scriptPath)));
                var method = exports.GetProperty(JavaScriptPropertyId.FromString(methodName));
                var input = ToJavaScriptValue(arg);

                if (_global.IsValid)
                {
                    input.SetProperty(JavaScriptPropertyId.FromString("__global"), _global, useStrictRules: true);
                }

                var output = method.CallFunction(JavaScriptValue.Undefined, input);

                return ToJToken(output);
            });
        }

        private T RunInContext<T>(Func<T> action)
        {
            Native.ThrowIfError(Native.JsSetCurrentContext(_context));

            try
            {
                return action();
            }
            catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
            {
                throw new JavaScriptEngineException($"{ex.ErrorCode}:\n{ToJToken(ex.Error)}");
            }
            finally
            {
                Native.ThrowIfError(Native.JsSetCurrentContext(JavaScriptContext.Invalid));
            }
        }

        private static JavaScriptContext CreateContext()
        {
            var flags = JavaScriptRuntimeAttributes.DisableBackgroundWork |
                        JavaScriptRuntimeAttributes.DisableEval |
                        JavaScriptRuntimeAttributes.EnableIdleProcessing;

            return JavaScriptRuntime.Create(flags, JavaScriptRuntimeVersion.VersionEdge).CreateContext();
        }

        private JavaScriptValue GetScriptExports(string scriptPath)
        {
            if (_scriptExports.TryGetValue(scriptPath, out var result))
            {
                return result;
            }

            t_modules.Value = new Dictionary<string, JavaScriptValue>(PathUtility.PathComparer);

            try
            {
                var exports = Run(scriptPath);

                // Avoid exports been GCed by javascript garbage collector.
                exports.AddRef();
                return _scriptExports[scriptPath] = exports;
            }
            finally
            {
                t_modules.Value = null;
            }
        }

        private static JavaScriptValue Run(string scriptPath)
        {
            var modules = t_modules.Value!;
            if (modules.TryGetValue(scriptPath, out var module))
            {
                return module;
            }

            var sourceCode = File.ReadAllText(scriptPath);
            var exports = modules[scriptPath] = JavaScriptValue.CreateObject();

            // add `process` to input to get the correct file path while running script inside docs-ui
            var script = $@"(function (module, exports, __dirname, require, process) {{{sourceCode}
}})";
            var dirname = Path.GetDirectoryName(scriptPath) ?? "";

            t_dirnames.Value!.Push(dirname);

            try
            {
                var sourceContext = JavaScriptSourceContext.FromIntPtr((IntPtr)Interlocked.Increment(ref s_currentSourceContext));

                JavaScriptContext.RunScript(script, sourceContext, scriptPath).CallFunction(
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
                var dirname = t_dirnames.Value!.Peek();
                var scriptPath = Path.GetFullPath(Path.Combine(dirname, arguments[1].ToString()));

                return Run(scriptPath);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                Native.JsSetException(JavaScriptValue.CreateError(JavaScriptValue.FromString(ex.Message)));
                return JavaScriptValue.Invalid;
            }
        }

        private static JavaScriptValue ToJavaScriptValue(JToken token)
        {
            switch (token)
            {
                case JArray arr:
                    var resultArray = JavaScriptValue.CreateArray((uint)arr.Count);
                    for (var i = 0; i < arr.Count; i++)
                    {
                        resultArray.SetIndexedProperty(
                            JavaScriptValue.FromInt32(i), ToJavaScriptValue(arr[i]));
                    }
                    return resultArray;

                case JObject obj:
                    var resultObj = JavaScriptValue.CreateObject();
                    foreach (var (name, value) in obj)
                    {
                        if (value != null)
                        {
                            resultObj.SetProperty(
                                JavaScriptPropertyId.FromString(name), ToJavaScriptValue(value), useStrictRules: true);
                        }
                    }
                    return resultObj;

                case JValue scalar:
                    switch (scalar.Value)
                    {
                        case null:
                            return JavaScriptValue.Null;

                        case bool aBool:
                            return JavaScriptValue.FromBoolean(aBool);

                        case string aString:
                            return JavaScriptValue.FromString(aString);

                        case DateTime aDate:
                            var constructor = JavaScriptValue.GlobalObject.GetProperty(JavaScriptPropertyId.FromString("Date"));
                            var args = new[] { JavaScriptValue.Undefined, JavaScriptValue.FromString(aDate.ToString("o", CultureInfo.InvariantCulture)) };
                            Native.ThrowIfError(Native.JsConstructObject(constructor, args, 2, out var date));
                            return date;

                        case long aInt:
                            return JavaScriptValue.FromInt32((int)aInt);

                        case double aDouble:
                            return JavaScriptValue.FromDouble(aDouble);
                    }
                    break;
            }

            throw new NotSupportedException($"Cannot marshal JToken type '{token.Type}'");
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
                    var arrLength = value.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
                    for (var i = 0; i < arrLength; i++)
                    {
                        arr.Add(ToJToken(value.GetIndexedProperty(JavaScriptValue.FromInt32(i))));
                    }
                    return arr;

                case JavaScriptValueType.Object:
                case JavaScriptValueType.Error:
                    var obj = new JObject();
                    var names = value.GetOwnPropertyNames();
                    var namesLength = names.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
                    for (var i = 0; i < namesLength; i++)
                    {
                        var name = names.GetIndexedProperty(JavaScriptValue.FromInt32(i)).ToString();
                        if (name != "__global")
                        {
                            obj[name] = ToJToken(value.GetProperty(JavaScriptPropertyId.FromString(name)));
                        }
                    }
                    return obj;

                default:
                    throw new NotSupportedException($"Cannot marshal javascript type '{value.ValueType}'");
            }
        }
    }
}
