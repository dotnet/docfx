// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine : IDisposable
    {
        private readonly string? _templateDir;
        private readonly string? _schemaDir;
        private readonly string? _contentTemplateDir;
        private readonly JObject? _global;
        private readonly LiquidTemplate? _liquid;
        private readonly ThreadLocal<IJavaScriptEngine>? _js;
        private readonly MustacheTemplate? _mustacheTemplate;
        private readonly TemplateDefinition? _templateDefinition;
        private readonly FileResolver _fileResolver;

        private readonly ConcurrentDictionary<string, JsonSchemaValidator?> _schemas
                   = new ConcurrentDictionary<string, JsonSchemaValidator?>(StringComparer.OrdinalIgnoreCase);

        public TemplateEngine(
            Config config,
            BuildOptions buildOptions,
            PackageResolver packageResolver,
            FileResolver fileResolver,
            ErrorBuilder errors,
            Lazy<JsonSchemaTransformer> jsonSchemaTransformer)
        {
            _fileResolver = fileResolver;

            if (config.Template.Type == PackageType.File)
            {
                _templateDefinition = JsonUtility.Deserialize<TemplateDefinition>(
                    errors,
                    fileResolver.ReadString(new SourceInfo<string>(config.Template.Url)),
                    new FilePath("--stdin"));
            }
            else
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
        }

        public bool IsHtml(ContentType contentType, string? mime)
        {
            if (_templateDefinition != null)
            {
                // TODO: For the page, we need the schema to tell whether it is a Data type page.
                return contentType switch
                {
                    ContentType.Redirection => true,
                    ContentType.Page => true,
                    ContentType.TableOfContents => true,
                    _ => false,
                };
            }
            Debug.Assert(_mustacheTemplate != null);
            return contentType switch
            {
                ContentType.Redirection => true,
                ContentType.Page => IsConceptual(mime) || IsLandingData(mime) || _mustacheTemplate!.HasTemplate($"{mime}.html"),
                ContentType.TableOfContents => _mustacheTemplate!.HasTemplate($"toc.html"),
                _ => false,
            };
        }

        public static bool IsConceptual(string? mime) => "Conceptual".Equals(mime, StringComparison.OrdinalIgnoreCase);

        public static bool IsLandingData(string? mime) => "LandingData".Equals(mime, StringComparison.OrdinalIgnoreCase);

        public static bool IsMigratedFromMarkdown(string? mime)
        {
            return "Hub".Equals(mime, StringComparison.OrdinalIgnoreCase) ||
                   "Landing".Equals(mime, StringComparison.OrdinalIgnoreCase) ||
                   "LandingData".Equals(mime, StringComparison.OrdinalIgnoreCase);
        }

        public JsonSchema GetSchema(SourceInfo<string?> schemaName)
        {
            return GetSchemaValidator(schemaName).Schema;
        }

        public JsonSchemaValidator GetSchemaValidator(SourceInfo<string?> schemaName)
        {
            var name = schemaName.Value ?? throw Errors.Yaml.SchemaNotFound(schemaName).ToException();

            return _schemas.GetOrAdd(name, GetSchemaCore) ?? throw Errors.Yaml.SchemaNotFound(schemaName).ToException();
        }

        public string RunLiquid(Document file, TemplateModel model)
        {
            Debug.Assert(_liquid != null);
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
            Debug.Assert(_mustacheTemplate != null);
            return _mustacheTemplate.Render(templateName, pageModel, file);
        }

        public JToken RunJavaScript(string scriptName, JObject model, string methodName = "transform")
        {
            Debug.Assert(_contentTemplateDir != null);
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
                    return JToken.Parse(content);
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
            return _global?[key]?.ToString();
        }

        public void Dispose()
        {
            _js?.Dispose();
        }

        private JObject LoadGlobalTokens()
        {
            Debug.Assert(_contentTemplateDir != null);
            var path = Path.Combine(_contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private JsonSchemaValidator? GetSchemaCore(string schemaName)
        {
            string schemaFilePath;
            if (IsLandingData(schemaName))
            {
                schemaFilePath = Path.Combine(AppContext.BaseDirectory, "data/schemas/LandingData.json");
            }
            else if (_templateDefinition != null)
            {
                if (!_templateDefinition.Definitions.TryGetValue(schemaName, out var schemaDefinition))
                {
                    return null;
                }
                schemaFilePath = _fileResolver.ResolveFilePath(new SourceInfo<string>(schemaDefinition));
            }
            else
            {
                Debug.Assert(_schemaDir != null);
                schemaFilePath = Path.Combine(_schemaDir, $"{schemaName}.schema.json");
            }

            if (!File.Exists(schemaFilePath))
            {
                return null;
            }

            var jsonSchema = JsonUtility.DeserializeData<JsonSchema>(File.ReadAllText(schemaFilePath), new FilePath(schemaFilePath));

            return new JsonSchemaValidator(jsonSchema, forceError: true);
        }
    }
}
