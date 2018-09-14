// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DotLiquid;
using DotLiquid.FileSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class LiquidTemplate
    {
        private readonly ConcurrentDictionary<string, Lazy<Template>> _templates = new ConcurrentDictionary<string, Lazy<Template>>();
        private readonly IncludeFileSystem _fileSystem;
        private readonly string _templateDir;

        public LiquidTemplate(string templateDir)
        {
            _templateDir = templateDir;
            _fileSystem = new IncludeFileSystem(templateDir);
        }

        public string Render(string templateName, JToken model)
        {
            var template = _templates.GetOrAdd(
                templateName,
                new Lazy<Template>(() => LoadTemplate(Path.Combine(_templateDir, templateName + ".html.liquid")))).Value;

            var parameters = new RenderParameters(CultureInfo.InvariantCulture)
            {
                Context = new DotLiquid.Context(
                    environments: new List<Hash> { (Hash)ToHash(model) },
                    outerScope: new Hash(),
                    registers: new Hash { ["file_system"] = _fileSystem },
                    errorsOutputMode: ErrorsOutputMode.Rethrow,
                    maxIterations: 0,
                    timeout: 0,
                    formatProvider: CultureInfo.InvariantCulture),
            };

            return template.Render(parameters);
        }

        private static Template LoadTemplate(string fullPath)
        {
            var template = Template.Parse(File.ReadAllText(fullPath));
            template.MakeThreadSafe();
            return template;
        }

        private static object ToHash(JToken token)
        {
            if (token == null)
                return null;

            if (token is JValue value)
                return value.Value;

            if (token is JArray arr)
                return new Hash((_, key) => ToHash(arr[key]));

            if (token is JObject obj)
                return new Hash((_, key) => ToHash(obj.GetValue(key)));

            throw new NotSupportedException($"Unknown jToken type {token.Type}");
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
