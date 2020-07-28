// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine : IDisposable
    {
        private readonly string _templateDir;
        private readonly string _schemaDir;
        private readonly string _contentTemplateDir;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly ThreadLocal<IJavaScriptEngine> _js;
        private readonly ConcurrentDictionary<string, TemplateSchema?> _schemas = new ConcurrentDictionary<string, TemplateSchema?>();
        private readonly MustacheTemplate _mustacheTemplate;

        public TemplateEngine(Config config, BuildOptions buildOptions, PackageResolver packageResolver, Lazy<JsonSchemaTransformer> jsonSchemaTransformer)
        {
            _templateDir = config.Template.Type switch
            {
                PackageType.None => Path.Combine(buildOptions.DocsetPath, "_themes"),
                _ => packageResolver.ResolvePackage(config.Template, PackageFetchOptions.DepthOne),
            };

            _contentTemplateDir = Path.Combine(_templateDir, "ContentTemplate");
            _schemaDir = Path.Combine(_contentTemplateDir, "schemas");

            _global = LoadGlobalTokens();
            _liquid = new LiquidTemplate(_templateDir);

            // TODO: remove JINT after Microsoft.CharkraCore NuGet package
            // supports linux and macOS: https://github.com/microsoft/ChakraCore/issues/2578
            _js = new ThreadLocal<IJavaScriptEngine>(() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? (IJavaScriptEngine)new ChakraCoreJsEngine(_contentTemplateDir, _global)
                : new JintJsEngine(_contentTemplateDir, _global));

            _mustacheTemplate = new MustacheTemplate(_contentTemplateDir, _global, jsonSchemaTransformer);
        }

        public bool IsHtml(ContentType contentType, string? mime)
        {
            return contentType switch
            {
                ContentType.Redirection => true,
                ContentType.Page => IsConceptual(mime) || IsLandingData(mime) || _mustacheTemplate.HasTemplate($"{mime}.html"),
                ContentType.TableOfContents => _mustacheTemplate.HasTemplate($"{mime}.html"),
                _ => false,
            };
        }

        public static bool IsConceptual(string? mime) => mime == "Conceptual";

        public static bool IsLandingData(string? mime) => mime == "LandingData";

        public static bool IsMigratedFromMarkdown(string? mime) => mime == "Hub" || mime == "Landing" || mime == "LandingData";

        public TemplateSchema GetSchema(SourceInfo<string?> schemaName)
        {
            var name = schemaName.Value ?? throw Errors.Yaml.SchemaNotFound(schemaName).ToException();

            return _schemas.GetOrAdd(name, GetSchemaCore) ?? throw Errors.Yaml.SchemaNotFound(schemaName).ToException();
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

        public string RunMustache(string templateName, JToken pageModel, FilePath file)
        {
            return _mustacheTemplate.Render(templateName, pageModel, file);
        }

        public JToken RunJavaScript(string scriptName, JObject model, string methodName = "transform")
        {
            var scriptPath = Path.Combine(_contentTemplateDir, scriptName);
            if (!File.Exists(scriptPath))
            {
                return model;
            }

            var result = _js.Value!.Run(scriptPath, methodName, model);
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

        public string? GetToken(string key)
        {
            return _global[key]?.ToString();
        }

        public void Dispose()
        {
            _js.Dispose();
        }

        private JObject LoadGlobalTokens()
        {
            var path = Path.Combine(_contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private TemplateSchema? GetSchemaCore(string schemaName)
        {
            var schemaFilePath = IsLandingData(schemaName)
                ? Path.Combine(AppContext.BaseDirectory, "data/schemas/LandingData.json")
                : Path.Combine(_schemaDir, $"{schemaName}.schema.json");

            if (!File.Exists(schemaFilePath))
            {
                return null;
            }

            var jsonSchema = JsonUtility.DeserializeData<JsonSchema>(File.ReadAllText(schemaFilePath), new FilePath(schemaFilePath));

            return new TemplateSchema(jsonSchema, new JsonSchemaValidator(jsonSchema, forceError: true));
        }
    }
}
