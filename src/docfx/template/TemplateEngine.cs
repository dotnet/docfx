// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine : IDisposable
    {
        private readonly string _templateDir;
        private readonly string _schemaDir;
        private readonly string _contentTemplateDir;
        private readonly Config _config;
        private readonly Output _output;
        private readonly TemplateDefinition _templateDefinition;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly ThreadLocal<JavaScriptEngine> _js;
        private readonly MustacheTemplate _mustacheTemplate;

        private readonly ConcurrentDictionary<string, JsonSchemaValidator?> _schemas
                   = new ConcurrentDictionary<string, JsonSchemaValidator?>(StringComparer.OrdinalIgnoreCase);

        public TemplateEngine(
            ErrorBuilder errors,
            Config config,
            BuildOptions buildOptions,
            Output output,
            PackageResolver packageResolver,
            Lazy<JsonSchemaTransformer> jsonSchemaTransformer)
        {
            _config = config;
            _output = output;

            _templateDir = config.Template.Type switch
            {
                PackageType.None => Path.Combine(buildOptions.DocsetPath, "_themes"),
                _ => packageResolver.ResolvePackage(config.Template, PackageFetchOptions.DepthOne),
            };

            _contentTemplateDir = Path.Combine(_templateDir, "ContentTemplate");
            _schemaDir = Path.Combine(_contentTemplateDir, "schemas");

            _templateDefinition = PathUtility.LoadYamlOrJson<TemplateDefinition>(errors, _templateDir, "template") ?? new TemplateDefinition();

            _global = LoadGlobalTokens();
            _liquid = new LiquidTemplate(_templateDir, _global);
            _js = new ThreadLocal<JavaScriptEngine>(() => JavaScriptEngine.Create(_contentTemplateDir, _global));
            _mustacheTemplate = new MustacheTemplate(_contentTemplateDir, _global, jsonSchemaTransformer);
        }

        public RenderType GetRenderType(ContentType contentType, SourceInfo<string?> mime)
        {
            return contentType switch
            {
                ContentType.Redirection => RenderType.Content,
                ContentType.Page => GetRenderType(mime),
                ContentType.TableOfContents => GetTocRenderType(),
                _ => RenderType.Component,
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

        public JsonSchema GetSchema(SourceInfo<string?> mime)
        {
            return GetSchemaValidator(mime).Schema;
        }

        public JsonSchemaValidator GetSchemaValidator(SourceInfo<string?> mime)
        {
            var name = mime.Value ?? throw Errors.Yaml.SchemaNotFound(mime).ToException();

            return _schemas.GetOrAdd(name, GetSchemaCore) ?? throw Errors.Yaml.SchemaNotFound(mime).ToException();
        }

        public string RunLiquid(SourceInfo<string?> mime, TemplateModel model)
        {
            var layout = model.RawMetadata?.Value<string>("layout") ?? mime.Value ?? throw new InvalidOperationException();
            var themeRelativePath = _templateDir;

            var liquidModel = new JObject
            {
                ["content"] = model.Content,
                ["page"] = model.RawMetadata,
                ["metadata"] = model.PageMetadata,
                ["theme_rel"] = themeRelativePath,
            };

            return _liquid.Render(layout, mime, liquidModel);
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
                    return JToken.Parse(content);
                }
                catch
                {
                    return result;
                }
            }
            return result;
        }

        public void CopyAssetsToOutput()
        {
            if (!_config.SelfContained || _templateDefinition.Assets.Length <= 0)
            {
                return;
            }

            var glob = GlobUtility.CreateGlobMatcher(_templateDefinition.Assets);

            Parallel.ForEach(PathUtility.GetFiles(_templateDir), file =>
            {
                if (glob(file))
                {
                    _output.Copy(file, new FilePath(Path.Combine(_templateDir, file)));
                }
            });
        }

        public string? GetToken(string key)
        {
            return _global[key]?.ToString();
        }

        public void Dispose()
        {
            _js.Dispose();
        }

        private RenderType GetRenderType(SourceInfo<string?> mime)
        {
            if (mime == null || IsConceptual(mime) || IsLandingData(mime))
            {
                return RenderType.Content;
            }
            try
            {
                return GetSchema(mime).RenderType;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var _))
            {
                return RenderType.Content;
            }
        }

        private RenderType GetTocRenderType()
        {
            try
            {
                return GetSchema(new SourceInfo<string?>("toc")).RenderType;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var _))
            {
                // TODO: Remove after schema of toc is support in template
                var isContentRenderType = _config.Template.Url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    || _config.Template.Path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                return isContentRenderType ? RenderType.Content : RenderType.Component;
            }
        }

        private JObject LoadGlobalTokens()
        {
            var path = Path.Combine(_contentTemplateDir, "token.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private JsonSchemaValidator? GetSchemaCore(string mime)
        {
            var schemaFilePath = IsLandingData(mime)
                ? Path.Combine(AppContext.BaseDirectory, "data/schemas/LandingData.json")
                : Path.Combine(_schemaDir, $"{mime}.schema.json");

            if (!File.Exists(schemaFilePath))
            {
                return null;
            }

            var jsonSchema = JsonUtility.DeserializeData<JsonSchema>(File.ReadAllText(schemaFilePath), new FilePath(schemaFilePath));

            // temporary mapping, will retired after we support config it in UI portal
            jsonSchema = LearnErrorMapping(mime, jsonSchema);

            return new JsonSchemaValidator(jsonSchema, forceError: true);
        }

        private static JsonSchema LearnErrorMapping(string mime, JsonSchema jsonSchema)
        {
            string mappingPath = mime switch
            {
                "LearningPath" => "data/schemas/learningpath-error-mapping.json",
                "Module" => "data/schemas/module-error-mapping.json",
                "ModuleUnit" => "data/schemas/moduleunit-error-mapping.json",
                _ => string.Empty,
            };

            if (!string.IsNullOrWhiteSpace(mappingPath))
            {
                var mapping = JsonUtility.DeserializeData<JsonSchema>(File.ReadAllText(mappingPath), null);

                LearnErrorMappingCore(jsonSchema, mapping);
            }

            return jsonSchema;
        }

        private static void LearnErrorMappingCore(JsonSchema jsonSchema, JsonSchema mapping)
        {
            foreach (var (propName, customRule) in mapping.Rules)
            {
                jsonSchema.Rules.Add(propName, customRule);
            }

            if (mapping.Items.schema != null)
            {
                switch (jsonSchema.Items)
                {
                    case (null, null):
                        jsonSchema.Items = (mapping.Items.schema, null);
                        break;
                    case (JsonSchema itemsSchema, _):
                        LearnErrorMappingCore(itemsSchema, mapping.Items.schema);
                        break;
                }
            }

            foreach (var (propName, subMapping) in mapping.Properties)
            {
                jsonSchema.Properties.TryGetValue(propName, out JsonSchema? subJsonSchema);

                if (subJsonSchema != null)
                {
                    LearnErrorMappingCore(subJsonSchema, subMapping);
                }
            }
        }
    }
}
