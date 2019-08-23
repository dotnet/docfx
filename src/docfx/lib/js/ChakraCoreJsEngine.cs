// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
        // A pool of ChakraCore runtime and context. Create one context for each runtime.
        private static readonly ConcurrentBag<JavaScriptContext> s_contextPool = new ConcurrentBag<JavaScriptContext>();

        // Limit the maximum ChakraCore runtimes to current processor count.
        private static readonly SemaphoreSlim s_contextThrottler = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

        private static readonly ConcurrentDictionary<(JavaScriptContext, string scriptPath), JavaScriptValue> s_scriptExports
                          = new ConcurrentDictionary<(JavaScriptContext, string scriptPath), JavaScriptValue>();

        private static readonly JavaScriptNativeFunction s_requireFunction = new JavaScriptNativeFunction(Require);

        private static readonly ThreadLocal<Stack<(string scriptPath, JavaScriptValue exports)>> t_stack =
                            new ThreadLocal<Stack<(string scriptPath, JavaScriptValue exports)>>(() => new Stack<(string, JavaScriptValue)>());

        private static readonly ThreadLocal<Dictionary<string, JavaScriptValue>> t_modules
                          = new ThreadLocal<Dictionary<string, JavaScriptValue>>(() => new Dictionary<string, JavaScriptValue>(PathUtility.PathComparer));

        private static JavaScriptSourceContext s_currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);

        private readonly ConcurrentDictionary<JavaScriptContext, JavaScriptValue> _globals = new ConcurrentDictionary<JavaScriptContext, JavaScriptValue>();

        private readonly string _scriptDir;
        private readonly JObject _global;

        public ChakraCoreJsEngine(string scriptDir, JObject global = null)
        {
            _scriptDir = scriptDir;
            _global = global;
        }

        public JToken Run(string scriptPath, string methodName, JToken arg)
        {
            s_contextThrottler.Wait();

            try
            {
                var context = s_contextPool.TryTake(out var existingContext) ? existingContext : CreateContext();

                using (new JavaScriptContext.Scope(context))
                {
                    try
                    {
                        var exports = GetScriptExports(context, Path.GetFullPath(Path.Combine(_scriptDir, scriptPath)));
                        var global = GetGlobal(context);
                        var method = exports.GetProperty(JavaScriptPropertyId.FromString(methodName));
                        var input = ToJavaScriptValue(arg);

                        if (global.IsValid)
                        {
                            input.SetProperty(JavaScriptPropertyId.FromString("__global"), global, useStrictRules: true);
                        }

                        var output = method.CallFunction(JavaScriptValue.Undefined, input);

                        return ToJToken(output);
                    }
                    catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
                    {
                        throw new JavaScriptEngineException($"{ex.ErrorCode}:\n{ToJToken(ex.Error)}");
                    }
                    finally
                    {
                        s_contextPool.Add(context);
                    }
                }
            }
            finally
            {
                s_contextThrottler.Release();
            }
        }

        private static JavaScriptContext CreateContext()
        {
            var flags = JavaScriptRuntimeAttributes.DisableBackgroundWork |
                        JavaScriptRuntimeAttributes.DisableEval |
                        JavaScriptRuntimeAttributes.EnableIdleProcessing;

            var context = JavaScriptRuntime.Create(flags, JavaScriptRuntimeVersion.VersionEdge).CreateContext();

            using (new JavaScriptContext.Scope(context))
            {
                // add `process` to input to get the correct file path while running script inside docs-ui
                JavaScriptValue.GlobalObject.SetProperty(
                    JavaScriptPropertyId.FromString("process"), JavaScriptValue.CreateObject(), useStrictRules: true);

                JavaScriptValue.GlobalObject.SetProperty(
                    JavaScriptPropertyId.FromString("require"), JavaScriptValue.CreateFunction(s_requireFunction), useStrictRules: true);
            }

            return context;
        }

        private static JavaScriptValue GetScriptExports(JavaScriptContext context, string scriptPath)
        {
            return s_scriptExports.GetOrAdd((context, scriptPath), key =>
            {
                var exports = Run(key.scriptPath);

                // Avoid exports been GCed by javascript garbarge collector.
                exports.AddRef();
                return exports;
            });
        }

        private JavaScriptValue GetGlobal(JavaScriptContext context)
        {
            if (_global is null)
            {
                return JavaScriptValue.Invalid;
            }

            return _globals.GetOrAdd(context, key =>
            {
                var global = ToJavaScriptValue(_global);

                // Avoid exports been GCed by javascript garbarge collector.
                global.AddRef();
                return global;
            });
        }

        private static JavaScriptValue Run(string scriptPath)
        {
            var modules = t_modules.Value;
            if (modules.TryGetValue(scriptPath, out var module))
            {
                return module;
            }

            var script = File.ReadAllText(scriptPath);
            var exports = modules[scriptPath] = JavaScriptValue.CreateObject();

            t_stack.Value.Push((scriptPath, exports));

            try
            {
                JavaScriptValue.GlobalObject.SetProperty(
                    JavaScriptPropertyId.FromString("exports"), exports, useStrictRules: true);

                JavaScriptContext.RunScript(script, s_currentSourceContext++, scriptPath);

                return exports;
            }
            finally
            {
                t_stack.Value.Pop();

                var previousExports = t_stack.Value.TryPeek(out var top) ? top.exports : JavaScriptValue.Undefined;

                JavaScriptValue.GlobalObject.SetProperty(
                    JavaScriptPropertyId.FromString("exports"), previousExports, useStrictRules: true);
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

            var currentScriptPath = t_stack.Value.Peek().scriptPath;
            var targetScriptPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(currentScriptPath), arguments[1].ToString()));

            return Run(targetScriptPath);
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
                        resultObj.SetProperty(
                            JavaScriptPropertyId.FromString(name), ToJavaScriptValue(value), useStrictRules: true);
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
                        obj[name] = ToJToken(value.GetProperty(JavaScriptPropertyId.FromString(name)));
                    }
                    return obj;

                default:
                    throw new NotSupportedException($"Cannot marshal javascript type '{value.ValueType}'");
            }
        }
    }
}
