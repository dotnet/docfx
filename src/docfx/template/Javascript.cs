// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Javascript
    {
        private static readonly Engine s_engine = new Engine();

        private readonly ConcurrentDictionary<string, ThreadLocal<Func<JObject, JObject>>> _scripts = new ConcurrentDictionary<string, ThreadLocal<Func<JObject, JObject>>>();
        private readonly string _scriptDir;

        public Javascript(string scriptDir) => _scriptDir = scriptDir;

        public JObject Run(string scriptName, JObject input)
        {
            var transform = _scripts.GetOrAdd(
                scriptName,
                key => new ThreadLocal<Func<JObject, JObject>>(() => LoadTransform(Path.Combine(_scriptDir, key))));

            Debug.Assert(transform != null);

            return transform.Value(input);
        }

        private static Func<JObject, JObject> LoadTransform(string scriptPath)
        {
            var exports = Load(scriptPath, new Dictionary<string, ObjectInstance>());
            var transform = exports.Get("transform");

            return model =>
            {
                var output = transform.Invoke(ToJsValue(model));
                var content = output.AsObject().Get("content").AsString();
                return JObject.Parse(content);
            };
        }

        private static ObjectInstance Load(string scriptPath, Dictionary<string, ObjectInstance> modules)
        {
            if (modules.TryGetValue(scriptPath, out var module))
            {
                return module;
            }

            var engine = new Engine();
            engine.SetValue("exports", MakeObject());
            engine.SetValue("require", new Func<string, ObjectInstance>(Require));
            engine.Execute(File.ReadAllText(scriptPath));

            return modules[scriptPath] = engine.GetValue("exports").AsObject();

            ObjectInstance Require(string path)
            {
                var absolutePath = path == "./op.common.js"
                    ? Path.Combine(AppContext.BaseDirectory, "data/op.common.js")
                    : Path.Combine(Path.GetDirectoryName(scriptPath), path);
                return Load(absolutePath, modules);
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
    }
}
