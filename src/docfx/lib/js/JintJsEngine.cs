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
    internal class JintJsEngine : IJavaScriptEngine
    {
        private static readonly Engine s_engine = new Engine();

        private readonly string _scriptDir;
        private readonly JsValue _global;
        private readonly ConcurrentDictionary<string, ThreadLocal<JsValue>> _scripts
                   = new ConcurrentDictionary<string, ThreadLocal<JsValue>>();

        public JintJsEngine(string scriptDir, JObject? global = null)
        {
            _scriptDir = scriptDir;
            _global = Parse(global?.ToString() ?? "{}");
        }

        public string Run(string scriptPath, string methodName, string arg, bool grepContent = false)
        {
            var scriptFullPath = Path.GetFullPath(Path.Combine(_scriptDir, scriptPath));
            var exports = _scripts.GetOrAdd(scriptFullPath, file => new ThreadLocal<JsValue>(() => Run(file))).Value!;
            var method = exports.AsObject().Get(methodName);

            var jsArg = Parse(arg);
            jsArg.AsObject()?.Put("__global", _global, throwOnError: true);

            try
            {
                var result = method.Invoke(jsArg);
                if (grepContent)
                {
                    return result.AsObject().Get("content").ToString();
                }
                return Stringify(result);
            }
            catch (JavaScriptException jse)
            {
                throw new JavaScriptEngineException(jse.Error.ToString() + "\n" + jse.CallStack);
            }
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

                // add process to input to get the correct file path while running script inside docs-ui
                var script = $@"
;(function (module, exports, __dirname, require, process) {{
{sourceCode}
}})
";
                var dirname = Path.GetDirectoryName(fullPath) ?? "";
                var require = new ClrFunctionInstance(engine, Require);

                var func = engine.Execute(script, parserOptions).GetCompletionValue();
                func.Invoke(MakeObject(), exports, dirname, require, MakeObject());
                return exports;

                JsValue Require(JsValue self, JsValue[] arguments)
                {
                    return RunCore(Path.Combine(dirname, arguments[0].AsString()));
                }
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

        private static JsValue Parse(string token)
        {
            return s_engine.Json.Parse(null, new[] { new JsValue(token) });
        }

        private static string Stringify(JsValue token)
        {
            if (token.IsObject())
            {
                token.AsObject().Delete("__global", throwOnError: false);
            }
            return s_engine.Json.Stringify(null, new[] { token }).AsString();
        }
    }
}
