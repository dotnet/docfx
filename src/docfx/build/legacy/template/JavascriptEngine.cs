// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Parser;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JavascriptEngine
    {
        /// <summary>
        /// A private exception type just to include javascript stack trace.
        /// </summary>
        private class JintException : Exception
        {
            public JintException(string message)
                : base(message) { }
        }

        private static readonly Engine s_engine = new Engine();

        private readonly string _scriptDir;
        private readonly ConcurrentDictionary<string, ThreadLocal<JsValue>> _scripts
                   = new ConcurrentDictionary<string, ThreadLocal<JsValue>>();

        public JavascriptEngine(string scriptDir) => _scriptDir = scriptDir;

        public JToken Run(string scriptPath, string methodName, params JToken[] args)
        {
            var scriptFullPath = Path.GetFullPath(Path.Combine(_scriptDir, scriptPath));
            var exports = _scripts.GetOrAdd(scriptFullPath, file => new ThreadLocal<JsValue>(() => Run(file))).Value;
            var method = exports.AsObject().Get(methodName);

            return ToJToken(Invoke(method, Array.ConvertAll(args, ToJsValue)));
        }

        private static JsValue Run(string entryScriptPath)
        {
            var modules = new Dictionary<string, JsValue>();

            return RunCore(entryScriptPath);

            JsValue RunCore(string path)
            {
                var fullPath = Path.GetFullPath(path);
                if (modules.TryGetValue(fullPath, out var module))
                {
                    return module;
                }

                var engine = new Engine(opt => opt.LimitRecursion(5000));
                var exports = modules[fullPath] = MakeObject();
                var sourceCode = File.ReadAllText(fullPath);
                var parserOptions = new ParserOptions { Source = fullPath };
                var script = $@"
;(function (module, exports, __dirname, require) {{
{sourceCode}
}})
";
                var dirname = Path.GetDirectoryName(fullPath);
                var require = new ClrFunctionInstance(engine, Require);

                var func = engine.Execute(script, parserOptions).GetCompletionValue();
                func.Invoke(MakeObject(), exports, dirname, require);
                return exports;

                JsValue Require(JsValue self, JsValue[] arguments)
                {
                    return RunCore(Path.Combine(dirname, arguments[0].AsString()));
                }
            }
        }

        private static JsValue Invoke(JsValue func, params JsValue[] args)
        {
            try
            {
                return func.Invoke(args);
            }
            catch (JavaScriptException jse)
            {
                throw new JintException(jse.Error.ToString() + "\n" + jse.CallStack);
            }
        }

        private static ObjectInstance MakeObject()
        {
            return s_engine.Object.Construct(Arguments.Empty);
        }

        private static ObjectInstance MakeArray()
        {
            return s_engine.Array.Construct(Arguments.Empty);
        }

        private static JsValue ToJsValue(JToken token)
        {
            if (token is JArray arr)
            {
                var result = MakeArray();
                foreach (var item in arr)
                {
                    s_engine.Array.PrototypeObject.Push(result, Arguments.From(ToJsValue(item)));
                }
                return result;
            }

            if (token is JObject obj)
            {
                var result = MakeObject();
                foreach (var (key, value) in obj)
                {
                    result.Put(key, ToJsValue(value), throwOnError: true);
                }
                return result;
            }

            return JsValue.FromObject(s_engine, ((JValue)token).Value);
        }

        private static JToken ToJToken(JsValue token)
        {
            return JToken.Parse(s_engine.Json.Stringify(null, new[] { token }).AsString());
        }
    }
}
