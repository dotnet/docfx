// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Esprima;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JintJsEngine : JavaScriptEngine
    {
        private readonly Engine _engine = new Engine();
        private readonly string _scriptDir;
        private readonly JsValue _global;
        private readonly Dictionary<string, JsValue> _scriptExports = new Dictionary<string, JsValue>();

        public JintJsEngine(string scriptDir, JObject? global = null)
        {
            _scriptDir = scriptDir;
            _global = ToJsValue(global ?? new JObject());
        }

        public override JToken Run(string scriptPath, string methodName, JToken arg)
        {
            var exports = GetScriptExports(Path.GetFullPath(Path.Combine(_scriptDir, scriptPath)));
            var method = exports.AsObject().Get(methodName);

            var jsArg = ToJsValue(arg);
            if (jsArg.IsObject())
            {
                jsArg.AsObject().Set("__global", _global);
            }

            try
            {
                return ToJToken(method.Invoke(jsArg));
            }
            catch (JavaScriptException jse)
            {
                throw new JavaScriptEngineException(jse.Error.ToString() + "\n" + jse.CallStack);
            }
        }

        private JsValue GetScriptExports(string scriptPath)
        {
            if (_scriptExports.TryGetValue(scriptPath, out var result))
            {
                return result;
            }

            return _scriptExports[scriptPath] = Run(scriptPath);
        }

        private JsValue Run(string entryScriptPath)
        {
            var modules = new Dictionary<string, JsValue>();

            return RunCore(entryScriptPath);

            JsValue RunCore(string path)
            {
                var fullPath = Path.GetFullPath(path);
                if (modules.TryGetValue(fullPath, out var result))
                {
                    return result;
                }

                var engine = new Engine(opt => opt.LimitRecursion(5000));
                var exports = MakeObject();
                var module = MakeObject();
                module.Set("exports", exports);

                var sourceCode = File.ReadAllText(fullPath);
                var parserOptions = new ParserOptions(fullPath);

                // add process to input to get the correct file path while running script inside docs-ui
                var script = $@"
;(function (module, exports, __dirname, require, process) {{
{sourceCode}
}})
";
                var dirname = Path.GetDirectoryName(fullPath) ?? "";
                var require = new ClrFunctionInstance(engine, "require", Require);

                var func = engine.Execute(script, parserOptions).GetCompletionValue();
                func.Invoke(module, exports, dirname, require, MakeObject());
                return modules[fullPath] = module.Get("exports");

                JsValue Require(JsValue self, JsValue[] arguments)
                {
                    return RunCore(Path.Combine(dirname, arguments[0].AsString()));
                }
            }
        }

        private ObjectInstance MakeObject()
        {
            return _engine.Object.Construct(Arguments.Empty);
        }

        private ObjectInstance MakeArray()
        {
            return _engine.Array.Construct(Arguments.Empty);
        }

        private JsValue ToJsValue(JToken token)
        {
            if (token is JArray arr)
            {
                var result = MakeArray();
                foreach (var item in arr)
                {
                    _engine.Array.PrototypeObject.Push(result, Arguments.From(ToJsValue(item)));
                }
                return result;
            }

            if (token is JObject obj)
            {
                var result = MakeObject();
                foreach (var (key, value) in obj)
                {
                    if (value != null)
                    {
                        result.Set(key, ToJsValue(value));
                    }
                }
                return result;
            }

            return JsValue.FromObject(_engine, ((JValue)token).Value);
        }

        private JToken ToJToken(JsValue token)
        {
            if (token.IsObject())
            {
                token.AsObject().Delete("__global");
            }
            return JToken.Parse(_engine.Json.Stringify(null, new[] { token }).AsString());
        }
    }
}
