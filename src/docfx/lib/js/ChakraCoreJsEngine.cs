// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
    internal class ChakraCoreJsEngine : IJavaScriptEngine
    {
        // A pool of ChakraCore runtime and context. Create one context for each runtime.
        private static readonly ConcurrentBag<JavaScriptContext> s_contextPool = new ConcurrentBag<JavaScriptContext>();

        // Limit the maximum ChakraCore runtimes to current processor count.
        private static readonly SemaphoreSlim s_contextThrottler = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

        private static readonly ConcurrentDictionary<(JavaScriptContext, string scriptPath), JavaScriptValue> s_scriptExports
                          = new ConcurrentDictionary<(JavaScriptContext, string scriptPath), JavaScriptValue>();

        private static readonly JavaScriptNativeFunction s_requireFunction = new JavaScriptNativeFunction(Require);

        private static readonly ThreadLocal<Stack<string>> t_dirnames = new ThreadLocal<Stack<string>>(() => new Stack<string>());

        private static readonly ThreadLocal<Dictionary<string, JavaScriptValue>?> t_modules = new ThreadLocal<Dictionary<string, JavaScriptValue>?>();

        private static int s_currentSourceContext;

        private readonly ConcurrentDictionary<JavaScriptContext, JavaScriptValue> _globals = new ConcurrentDictionary<JavaScriptContext, JavaScriptValue>();

        private readonly string _scriptDir;
        private readonly JObject? _global;

        public ChakraCoreJsEngine(string scriptDir, JObject? global = null)
        {
            _scriptDir = scriptDir;
            _global = global;
        }

        public string Run(string scriptPath, string methodName, string arg, bool grepContent = false)
        {
            s_contextThrottler.Wait();

            try
            {
                var context = s_contextPool.TryTake(out var existingContext) ? existingContext : CreateContext();

                Native.ThrowIfError(Native.JsSetCurrentContext(context));

                try
                {
                    var json = JavaScriptValue.GlobalObject.GetProperty(JavaScriptPropertyId.FromString("JSON"));
                    var parse = json.GetProperty(JavaScriptPropertyId.FromString("parse"));
                    var stringify = json.GetProperty(JavaScriptPropertyId.FromString("stringify"));
                    var input = parse.CallFunction(JavaScriptValue.Undefined, JavaScriptValue.FromString(arg));

                    var exports = GetScriptExports(context, Path.GetFullPath(Path.Combine(_scriptDir, scriptPath)));
                    var global = GetGlobal(context);
                    var method = exports.GetProperty(JavaScriptPropertyId.FromString(methodName));

                    if (global.IsValid)
                    {
                        input.SetProperty(JavaScriptPropertyId.FromString("__global"), global, useStrictRules: true);
                    }

                    var result = method.CallFunction(JavaScriptValue.Undefined, input);
                    if (grepContent)
                    {
                        return result.GetProperty(JavaScriptPropertyId.FromString("content")).ToString();
                    }

                    return stringify.CallFunction(JavaScriptValue.Undefined, result).ToString();
                }
                catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
                {
                    throw new JavaScriptEngineException($"{ex.ErrorCode}:\n{Stringify(ex.Error)}");
                }
                finally
                {
                    Native.ThrowIfError(Native.JsSetCurrentContext(JavaScriptContext.Invalid));
                    s_contextPool.Add(context);
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

            return JavaScriptRuntime.Create(flags, JavaScriptRuntimeVersion.VersionEdge).CreateContext();
        }

        private static JavaScriptValue GetScriptExports(JavaScriptContext context, string scriptPath)
        {
            return s_scriptExports.GetOrAdd((context, scriptPath), key =>
            {
                t_modules.Value = new Dictionary<string, JavaScriptValue>(PathUtility.PathComparer);

                try
                {
                    var exports = Run(key.scriptPath);

                    // Avoid exports been GCed by javascript garbarge collector.
                    exports.AddRef();
                    return exports;
                }
                finally
                {
                    t_modules.Value = null;
                }
            });
        }

        private JavaScriptValue GetGlobal(JavaScriptContext context)
        {
            if (_global is null)
            {
                return JavaScriptValue.Undefined;
            }

            return _globals.GetOrAdd(context, key =>
            {
                var global = Parse(_global.ToString());

                // Avoid exports been GCed by javascript garbarge collector.
                global.AddRef();
                return global;
            });
        }

        private static JavaScriptValue Run(string scriptPath)
        {
            var modules = t_modules.Value!;
            if (modules.TryGetValue(scriptPath, out var module))
            {
                return module;
            }

            var exports = modules[scriptPath] = JavaScriptValue.CreateObject();
            var sourceCode = File.ReadAllText(scriptPath);

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

            var dirname = t_dirnames.Value!.Peek();
            var scriptPath = Path.GetFullPath(Path.Combine(dirname, arguments[1].ToString()));

            return Run(scriptPath);
        }

        private static JavaScriptValue Parse(string value)
        {
            var json = JavaScriptValue.GlobalObject.GetProperty(JavaScriptPropertyId.FromString("JSON"));
            var parse = json.GetProperty(JavaScriptPropertyId.FromString("parse"));
            var stringify = json.GetProperty(JavaScriptPropertyId.FromString("stringify"));
            return parse.CallFunction(JavaScriptValue.Undefined, JavaScriptValue.FromString(value));
        }

        private static string Stringify(JavaScriptValue value)
        {
            switch (value.ValueType)
            {
                case JavaScriptValueType.Error:
                    var obj = JavaScriptValue.CreateObject();
                    var names = value.GetOwnPropertyNames();
                    var namesLength = names.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
                    for (var i = 0; i < namesLength; i++)
                    {
                        var name = JavaScriptPropertyId.FromString(names.GetIndexedProperty(JavaScriptValue.FromInt32(i)).ToString());
                        obj.SetProperty(name, value.GetProperty(name), useStrictRules: true);
                    }
                    return Stringify(obj);

                default:
                    var json = JavaScriptValue.GlobalObject.GetProperty(JavaScriptPropertyId.FromString("JSON"));
                    var stringify = json.GetProperty(JavaScriptPropertyId.FromString("stringify"));
                    return stringify.CallFunction(JavaScriptValue.Undefined, value).ToString();
            }
        }
    }
}
