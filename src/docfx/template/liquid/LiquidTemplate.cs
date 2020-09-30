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
        private readonly Package _package;
        private readonly PackageFileSystem _fileSystem;
        private readonly IReadOnlyDictionary<string, string> _localizedStrings;

        private readonly ConcurrentDictionary<PathString, Lazy<Template?>> _templates = new ConcurrentDictionary<PathString, Lazy<Template?>>();

        [ThreadStatic]
        private static Package? t_package;

        static LiquidTemplate()
        {
            Template.RegisterTag<StyleTag>("style");
            Template.RegisterTag<JavaScriptTag>("js");
            Template.RegisterTag<LocalizeTag>("loc");
        }

        public LiquidTemplate(Package package, JObject? global = null)
        {
            _package = package;
            _fileSystem = new PackageFileSystem(LoadTemplate);
            _localizedStrings = global is null
                ? new Dictionary<string, string>()
                : global.Properties().ToDictionary(p => p.Name, p => p.Value.ToString());
        }

        public string Render(string templateName, SourceInfo<string?> mime, JObject model)
        {
            var template = LoadTemplate(new PathString($"{templateName}.html.liquid"))
                ?? throw Errors.Template.LiquidNotFound(mime).ToException(isError: false);

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

            try
            {
                t_package = _package;
                return template.Render(parameters);
            }
            finally
            {
                t_package = default;
            }
        }

        public static string? GetThemeRelativePath(string resourcePath)
        {
            return t_package?.TryGetPhysicalPath(new PathString(resourcePath));
        }

        private Template? LoadTemplate(PathString path)
        {
            return _templates.GetOrAdd(path, new Lazy<Template?>(() => LoadTemplateCore(path))).Value;
        }

        private Template? LoadTemplateCore(PathString path)
        {
            var content = _package.TryReadString(path);
            if (content is null)
            {
                return null;
            }

            var template = Template.Parse(content);
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

        private class PackageFileSystem : ITemplateFileSystem
        {
            private readonly Func<PathString, Template?> _loadTemplate;

            public PackageFileSystem(Func<PathString, Template?> loadTemplate) => _loadTemplate = loadTemplate;

            public Template GetTemplate(DotLiquid.Context context, string templateName) =>
                _loadTemplate(new PathString($"_includes/{templateName}.liquid")) ?? throw new FileNotFoundException($"_includes/{templateName}.liquid");

            public string ReadTemplateFile(DotLiquid.Context context, string templateName) => throw new NotSupportedException();
        }
    }
}
