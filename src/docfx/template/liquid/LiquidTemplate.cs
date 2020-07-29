// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DotLiquid;
using DotLiquid.FileSystems;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class LiquidTemplate
    {
        private readonly ConcurrentDictionary<string, Lazy<Template?>> _templates = new ConcurrentDictionary<string, Lazy<Template?>>();
        private readonly IncludeFileSystem _fileSystem;
        private readonly string _templateDir;
        private readonly IReadOnlyDictionary<string, string> _localizedStrings;

        static LiquidTemplate()
        {
            Template.RegisterTag<StyleTag>("style");
            Template.RegisterTag<JavaScriptTag>("js");
            Template.RegisterTag<LocalizeTag>("loc");
        }

        public LiquidTemplate(string templateDir)
        {
            _templateDir = templateDir;
            _fileSystem = new IncludeFileSystem(templateDir);
            _localizedStrings = LoadLocalizedStrings(templateDir);
        }

        public string Render(string templateName, SourceInfo<string?> mimeType, JObject model)
        {
            var template = _templates.GetOrAdd(
                templateName,
                new Lazy<Template?>(() =>
                {
                    var fileName = $"{templateName}.html.liquid";
                    if (!File.Exists(Path.Combine(_templateDir, fileName)))
                    {
                        return null;
                    }
                    return LoadTemplate(Path.Combine(_templateDir, fileName));
                })).Value;

            if (template is null)
            {
                throw Errors.Template.LiquidNotFound(mimeType).ToException(isError: false);
            }

            var registers = new Hash
            {
                ["file_system"] = _fileSystem,
                ["localized_strings"] = _localizedStrings,
            };

            var environments = new List<Hash>
            {
                Hash.FromDictionary((Dictionary<string, object>)ToLiquidObject(model)!),
            };

            var parameters = new RenderParameters(CultureInfo.InvariantCulture)
            {
                Context = new DotLiquid.Context(
                    environments: environments,
                    outerScope: new Hash(),
                    registers: registers,
                    errorsOutputMode: ErrorsOutputMode.Rethrow,
                    maxIterations: 0,
                    timeout: 0,
                    formatProvider: CultureInfo.InvariantCulture),
            };

            return template.Render(parameters);
        }

        public static string GetThemeRelativePath(DotLiquid.Context context, string resourcePath)
        {
            return Path.Combine((string)context["theme_rel"], resourcePath);
        }

        private static IReadOnlyDictionary<string, string> LoadLocalizedStrings(string templateDir)
        {
            var file = Path.Combine(templateDir, "yml/Conceptual.html.yml");
            if (!File.Exists(file))
            {
                return new Dictionary<string, string>();
            }

            var data = YamlUtility.DeserializeData<JArray>(File.ReadAllText(file), new FilePath(file));

            return data.ToDictionary(item => item.Value<string>("uid"), item => item.Value<string>("name"));
        }

        private static Template LoadTemplate(string fullPath)
        {
            var template = Template.Parse(File.ReadAllText(fullPath));
            template.MakeThreadSafe();
            return template;
        }

        private static object? ToLiquidObject(JToken token)
        {
            return token switch
            {
                JValue value => value.Value,
                JArray arr => arr.Select(ToLiquidObject).ToArray(),
                JObject obj => obj.Cast<KeyValuePair<string, JToken>>()
                                  .GroupBy(prop => prop.Key, StringComparer.OrdinalIgnoreCase)
                                  .ToDictionary(group => group.Key, group => ToLiquidObject(group.Last().Value)),
                _ => throw new NotSupportedException($"Unknown jToken type {token.Type}"),
            };
        }

        private class IncludeFileSystem : ITemplateFileSystem
        {
            private readonly string _templateDir;
            private readonly ConcurrentDictionary<string, Lazy<Template>> _templates = new ConcurrentDictionary<string, Lazy<Template>>();

            public IncludeFileSystem(string templateDir) => _templateDir = templateDir;

            public string ReadTemplateFile(DotLiquid.Context context, string templateName) => throw new NotSupportedException();

            public Template GetTemplate(DotLiquid.Context context, string templateName)
            {
                return _templates.GetOrAdd(
                    templateName,
                    new Lazy<Template>(() => LoadTemplate(Path.Combine(_templateDir, "_includes", templateName + ".liquid")))).Value;
            }
        }
    }
}
