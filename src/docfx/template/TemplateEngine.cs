// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        private readonly string _templateDir;
        private readonly string _contentTemplateDir;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly IJavaScriptEngine _js;
        private readonly IReadOnlyDictionary<string, Lazy<TemplateSchema>> _schemas;
        private readonly MustacheTemplate _mustacheTemplate;
        private readonly Config _config;

        public TemplateEngine(Config config, BuildOptions buildOptions, PackageResolver packageResolver)
        {
            _config = config;
            _templateDir = config.Template.Type switch
            {
                PackageType.None => Path.Combine(buildOptions.DocsetPath, "_themes"),
                _ => packageResolver.ResolvePackage(config.Template, PackageFetchOptions.DepthOne),
            };

            _contentTemplateDir = Path.Combine(_templateDir, "ContentTemplate");
            var schemaDir = Path.Combine(_contentTemplateDir, "schemas");

            _global = LoadGlobalTokens();
            _schemas = LoadSchemas(schemaDir, _contentTemplateDir);
            _liquid = new LiquidTemplate(_templateDir);

            // TODO: remove JINT after Microsoft.CharkraCore NuGet package
            // supports linux and macOS: https://github.com/microsoft/ChakraCore/issues/2578
            _js = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (IJavaScriptEngine)new ChakraCoreJsEngine(_contentTemplateDir, _global)
                : new JintJsEngine(_contentTemplateDir, _global);

            _mustacheTemplate = new MustacheTemplate(_contentTemplateDir, _global);
        }

        public bool IsPage(string? mime)
        {
            return mime is null || !_schemas.TryGetValue(mime, out var schemaTemplate) || schemaTemplate.Value.IsPage;
        }

        public static bool IsConceptual(string? mime)
        {
            return string.Equals(mime, "Conceptual", StringComparison.OrdinalIgnoreCase);
        }

        public TemplateSchema GetSchema(SourceInfo<string?> schemaName)
        {
            var name = schemaName.Value;
            return !string.IsNullOrEmpty(name) && _schemas.TryGetValue(name, out var schemaTemplate)
               ? schemaTemplate.Value
               : throw Errors.Yaml.SchemaNotFound(schemaName).ToException();
        }

        public string RunLiquid(Document file, TemplateModel model)
        {
            var layout = model.RawMetadata?.Value<string>("layout") ?? "";
            var themeRelativePath = _templateDir;

            var liquidModel = new JObject
            {
                ["content"] = model.Content,
                ["page"] = model.RawMetadata,
                ["metadata"] = model.PageMetadata,
                ["theme_rel"] = themeRelativePath,
            };

            return _liquid.Render(layout, file.Mime, liquidModel);
        }

        public string RunMustache(string templateName, JToken pageModel)
        {
            return _mustacheTemplate.Render(templateName, pageModel);
        }

        public JToken RunJavaScript(string scriptName, JObject model, string methodName = "transform")
        {
            var scriptPath = Path.Combine(_contentTemplateDir, scriptName);
            if (!File.Exists(scriptPath))
            {
                return model;
            }

            var result = _js.Run(scriptPath, methodName, model);
            if (result is JObject obj && obj.TryGetValue("content", out var token) &&
                token is JValue value && value.Value is string content)
            {
                try
                {
                    return JObject.Parse(content);
                }
                catch
                {
                    return result;
                }
            }
            return result;
        }

        public static bool IsLandingData(string? mime)
        {
            return mime != null && string.Equals(typeof(LandingData).Name, mime, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMigratedFromMarkdown(string? mime)
        {
            var migratedMimeTypes = new string[] { "Hub", "Landing", nameof(LandingData) };
            return mime != null && migratedMimeTypes.Contains(mime, StringComparer.OrdinalIgnoreCase);
        }

        public string? GetToken(string key)
        {
            return _global[key]?.ToString();
        }

        private JObject LoadGlobalTokens()
        {
            var path = Path.Combine(_contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private IReadOnlyDictionary<string, Lazy<TemplateSchema>>
            LoadSchemas(string schemaDir, string contentTemplateDir)
        {
            var schemas = Directory.Exists(schemaDir)
                ? (from k in Directory.EnumerateFiles(schemaDir, "*.schema.json", SearchOption.TopDirectoryOnly)
                   let fileName = Path.GetFileName(k)
                   select fileName.Substring(0, fileName.Length - ".schema.json".Length))
                   .ToDictionary(
                    schemaName => schemaName, schemaName => new Lazy<TemplateSchema>(() => new TemplateSchema(schemaName, schemaDir, contentTemplateDir)))
                : new Dictionary<string, Lazy<TemplateSchema>>();

            schemas.Add("LandingData", new Lazy<TemplateSchema>(() => new TemplateSchema("LandingData", schemaDir, contentTemplateDir)));
            return schemas;
        }
    }
}
