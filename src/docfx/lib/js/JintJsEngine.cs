// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Runtime.Interop;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal sealed class JintJsEngine : JavaScriptEngine
{
    private readonly Engine _engine = new();
    private readonly Package _package;
    private readonly JsValue _global;
    private readonly Dictionary<PathString, JsValue> _modules = new();

    public JintJsEngine(Package package, JObject? global = null)
    {
        _package = package;
        _global = ToJsValue(global ?? new());
    }

    public override JToken Run(string scriptPath, string methodName, JToken arg)
    {
        var exports = Run(new(scriptPath));
        var method = exports.AsObject().Get(methodName);

        var jsArg = ToJsValue(arg);
        if (jsArg.IsObject())
        {
            jsArg.AsObject().Set("__global", _global);
        }

        return ToJToken(_engine.Invoke(method, jsArg));
    }

    public override void Dispose()
    {
        _engine.Dispose();
    }

    private JsValue Run(PathString scriptPath)
    {
        if (_modules.TryGetValue(scriptPath, out var result))
        {
            return result;
        }

        using var engine = new Engine(opt => opt.LimitRecursion(5000));
        var exports = MakeObject();
        var module = MakeObject();
        module.Set("exports", exports);

        var sourceCode = _package.ReadString(scriptPath);

        // add process to input to get the correct file path while running script inside docs-ui
        var script = $@"
;(function (module, exports, __dirname, require, process) {{
{sourceCode}
}})
";
        var dirname = Path.GetDirectoryName(scriptPath) ?? "";
        var require = new ClrFunctionInstance(engine, "require", Require);

        var func = engine.Evaluate(script, scriptPath);
        engine.Invoke(func, module, exports, dirname, require, MakeObject());
        return _modules[scriptPath] = module.Get("exports");

        JsValue Require(JsValue self, JsValue[] arguments)
        {
            return Run(new(Path.Combine(dirname, arguments[0].AsString())));
        }
    }

    private JsObject MakeObject()
    {
        return new JsObject(_engine);
    }

    private JsValue ToJsValue(JToken token)
    {
        if (token is JArray arr)
        {
            var result = new JsArray(_engine, (uint)arr.Count);
            foreach (var item in arr)
            {
                result.Push(ToJsValue(item));
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
                    result.FastSetDataProperty(key, ToJsValue(value));
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
        return JToken.Parse(new JsonSerializer(_engine).Serialize(token, JsValue.Null, JsValue.Null).AsString());
    }
}
