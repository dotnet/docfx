// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stubble.Compilation;
using Stubble.Compilation.Builders;
using Stubble.Compilation.Settings;
using Stubble.Core.Interfaces;

namespace Microsoft.Docs.Build
{
    internal class MustacheTemplate
    {
        private readonly string _templateDir;
        private readonly MethodInfo? _method;
        private readonly JObject? _global;
        private readonly StubbleCompilationRenderer _renderer;
        private readonly ConcurrentDictionary<string, Lazy<Func<JToken, string>?>> _templates = new ConcurrentDictionary<string, Lazy<Func<JToken, string>?>>();

        public MustacheTemplate(string templateDir, JObject? global = null)
        {
            _templateDir = templateDir;
            _method = GetType().GetMethod(nameof(GetJObjectValue), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _global = global;
            _renderer = new StubbleCompilationBuilder().Configure(
                settings => UseJson(settings).SetPartialTemplateLoader(new PartialLoader(templateDir))).Build();
        }

        public string Render(string templateFileName, JToken model)
        {
            var template = _templates.GetOrAdd(
                templateFileName,
                key => new Lazy<Func<JToken, string>?>(() =>
                {
                    var fileName = Path.Combine(_templateDir, templateFileName);
                    return File.Exists(fileName)
                    ? _renderer.Compile<JToken>(MustacheXrefTagParser.ProcessXrefTag(File.ReadAllText(fileName).Replace("\r", "")))
                    : null;
                })).Value;

            return template == null ? JsonUtility.Serialize(model) : template(model);
        }

        private CompilerSettingsBuilder UseJson(CompilerSettingsBuilder settings)
        {
            // JObject implements IEnumerable, stubble treats IEnumerable as array,
            // need to put it to section blacklist and overwrite the truthy check method.
            return settings.AddValueGetter<JObject>(GetValue)
                           .AddValueGetter<JValue>(GetValue)
                           .AddTruthyCheck<JObject>(value => value != null)
                           .AddTruthyCheck<JValue>(value => value.Type != JTokenType.Null)
                           .AddSectionBlacklistType(typeof(JObject))
                           .AddSectionBlacklistType(typeof(JValue));
        }

        private Expression GetValue(Type type, Expression instance, string key, bool ignoreCase)
        {
            return Expression.Call(Expression.Constant(this), _method, instance, Expression.Constant(key), Expression.Constant(ignoreCase));
        }

        private object? GetJObjectValue(JToken token, string key, bool ignoreCase)
        {
            switch (token)
            {
                case JObject obj:
                    if (key == "__global" && _global != null)
                    {
                        return _global;
                    }
                    var childToken = obj.GetValue(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

                    return childToken is JValue scalar ? scalar.Value ?? JValue.CreateNull() : childToken;
                case JValue val:
                    return key.Trim() == "." ? ((JValue)val).Value : null;
            }
            return default;
        }

        private class PartialLoader : IStubbleLoader
        {
            private readonly string _dir;
            private readonly ConcurrentDictionary<string, Lazy<string>> _cache = new ConcurrentDictionary<string, Lazy<string>>();

            public PartialLoader(string dir) => _dir = dir;

            public IStubbleLoader Clone() => new PartialLoader(_dir);

            public ValueTask<string> LoadAsync(string name) => new ValueTask<string>(Load(name));

            public string Load(string name) => _cache.GetOrAdd(
                name,
                key => new Lazy<string>(() =>
                {
                    var fileName = Path.Combine(_dir, name);
                    if (!File.Exists(fileName))
                    {
                        fileName = Path.Combine(_dir, name + ".tmpl.partial");
                    }

                    return MustacheXrefTagParser.ProcessXrefTag(File.ReadAllText(fileName).Replace("\r", ""));
                })).Value;
        }
    }
}
