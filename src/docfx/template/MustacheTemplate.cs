// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stubble.Core.Builders;
using Stubble.Core.Interfaces;
using Stubble.Core.Settings;

namespace Microsoft.Docs.Build
{
    internal class MustacheTemplate
    {
        private readonly string _templateDir;
        private readonly IStubbleRenderer _renderer;
        private readonly ConcurrentDictionary<string, Lazy<string?>> _templates = new ConcurrentDictionary<string, Lazy<string?>>();

        public MustacheTemplate(string templateDir, JObject? global = null)
        {
            _templateDir = templateDir;
            _renderer = new StubbleBuilder().Configure(
                settings => UseJson(settings, global).SetPartialTemplateLoader(new PartialLoader(templateDir))).Build();
        }

        public string Render(string templateFileName, JToken model)
        {
            var template = _templates.GetOrAdd(
                templateFileName,
                key => new Lazy<string?>(() =>
                {
                    var fileName = Path.Combine(_templateDir, templateFileName);
                    return File.Exists(fileName)
                    ? MustacheXrefTagParser.ProcessXrefTag(File.ReadAllText(fileName).Replace("\r", ""))
                    : null;
                })).Value;

            return template == null ? JsonUtility.Serialize(model) : _renderer.Render(template, model);
        }

        private static RendererSettingsBuilder UseJson(RendererSettingsBuilder settings, JObject? global)
        {
            // JObject implements IEnumerable, stubble treats IEnumerable as array,
            // need to put it to section blacklist and overwrite the truthy check method.
            return settings.AddValueGetter(typeof(JObject), GetJObjectValue)
                           .AddValueGetter(typeof(JValue), GetJValueValue)
                           .AddTruthyCheck<string>(value => !string.IsNullOrEmpty(value))
                           .AddTruthyCheck<JObject>(value => value != null)
                           .AddTruthyCheck<JValue>(value => value.Type != JTokenType.Null)
                           .AddSectionBlacklistType(typeof(JObject))
                           .AddSectionBlacklistType(typeof(JValue));

            object? GetJObjectValue(object value, string key, bool ignoreCase)
            {
                if (key == "__global" && global != null)
                {
                    return global;
                }
                var token = (JObject)value;
                var childToken = token.GetValue(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

                return childToken is JValue scalar ? scalar.Value ?? JValue.CreateNull() : childToken;
            }

            object? GetJValueValue(object value, string key, bool ignoreCase)
                => key.Trim() == "." ? ((JValue)value).Value : null;
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
