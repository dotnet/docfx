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
        private readonly ConcurrentDictionary<string, Lazy<string>> _templates = new ConcurrentDictionary<string, Lazy<string>>();

        public MustacheTemplate(string templateDir)
        {
            _templateDir = templateDir;
            _renderer = new StubbleBuilder().Configure(
                settings => UseJson(settings).SetPartialTemplateLoader(new PartialLoader(templateDir))).Build();

            RendererSettingsBuilder UseJson(RendererSettingsBuilder settings)
            {
                return settings.AddValueGetter(typeof(JObject), (value, key, ignoreCase) =>
                {
                    var token = (JObject)value;
                    var childToken = token.GetValue(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

                    return childToken is JValue scalar ? scalar.Value : childToken;
                });
            }
        }

        public string Render(string templateName, JToken model)
        {
            var template = _templates.GetOrAdd(
                templateName,
                key => new Lazy<string>(() => File.ReadAllText(Path.Combine(_templateDir, key + ".html.primary.tmpl")))).Value;

            return _renderer.Render(template, model);
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
                key => new Lazy<string>(() => File.ReadAllText(Path.Combine(_dir, name + ".tmpl.partial")))).Value;
        }
    }
}
