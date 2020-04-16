// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stubble.Core;
using Stubble.Core.Builders;
using Stubble.Core.Classes;
using Stubble.Core.Interfaces;
using Stubble.Core.Parser;
using Stubble.Core.Renderers;
using Stubble.Core.Settings;

namespace Microsoft.Docs.Build
{
    internal class MustacheTemplate
    {
        private readonly JObject? _global;
        private readonly IStubbleRenderer _renderer;

        public MustacheTemplate(string templateDir, JObject? global = null)
        {
            var templateLoader = new TemplateLoader(templateDir);

            var valueGetters = new Dictionary<Type, RendererSettingsDefaults.ValueGetterDelegate>
            {
                [typeof(JToken)] = GetValue,
            };

            var truthyCheck = new List<Func<object, bool>> { IsTruthy };
            var truthyChecks = new Dictionary<Type, List<Func<object, bool>>>
            {
                [typeof(JObject)] = truthyCheck,
                [typeof(JArray)] = truthyCheck,
                [typeof(JValue)] = truthyCheck,
            };

            var enumerationConverters = new Dictionary<Type, Func<object, IEnumerable>>();

            // JObject implements IEnumerable, stubble treats IEnumerable as array,
            // need to put it to section blacklist and overwrite the truthy check method.
            var sectionBlacklistTypes = new HashSet<Type>
            {
                typeof(JObject), typeof(JValue), typeof(string), typeof(IDictionary),
            };

            var rendererSettings = new RendererSettings(
                valueGetters,
                truthyChecks,
                templateLoader,
                templateLoader,
                maxRecursionDepth: 256,
                new RenderSettings { SkipRecursiveLookup = true },
                enumerationConverters,
                ignoreCaseOnLookup: true,
                new CachedMustacheParser(),
                new TokenRendererPipeline<Stubble.Core.Contexts.Context>(RendererSettingsDefaults.DefaultTokenRenderers()),
                new Tags("{{", "}}"),
                new ParserPipelineBuilder().Build(),
                sectionBlacklistTypes,
                EncodingFunctions.WebUtilityHtmlEncoding);

            _global = global;
            _renderer = new StubbleVisitorRenderer(rendererSettings);
        }

        public string Render(string templateFileName, JToken model)
        {
            return _renderer.Render(templateFileName, model);
        }

        private bool IsTruthy(object token)
        {
            return token switch
            {
                JArray array => array.Count > 0,
                JValue value => value.Value switch
                {
                    null => false,
                    string stringValue => stringValue.Length > 0,
                    bool boolValue => boolValue,
                    int intValue => intValue != 0,
                    long longValue => longValue != 0,
                    float floatValue => floatValue != 0,
                    double doubleValue => doubleValue != 0,
                    _ => true,
                },
                _ => true,
            };
        }

        private object? GetValue(object token, string key, bool ignoreCase)
        {
            return token switch
            {
                JObject obj => key == "__global" && _global != null
                    ? _global : obj.GetValue(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal),
                JArray array when int.TryParse(key, out var index) && index >= 0 && index < array.Count => array[index],
                _ => null,
            };
        }

        private class TemplateLoader : IStubbleLoader
        {
            private readonly string _dir;
            private readonly ConcurrentDictionary<string, Lazy<string>> _cache = new ConcurrentDictionary<string, Lazy<string>>();

            public TemplateLoader(string dir) => _dir = dir;

            public IStubbleLoader Clone() => new TemplateLoader(_dir);

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
