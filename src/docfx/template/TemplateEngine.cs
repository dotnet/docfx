// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        public TemplateEngineRunner TemplateEngineRunner { get; init; }

        private readonly Config _config;
        private readonly Package _package;
        private readonly Lazy<TemplateDefinition> _templateDefinition;
        private readonly JObject _global;
        private readonly BuildOptions? _buildOptions;
        private readonly JsonSchemaLoader _jsonSchemaLoader;

        private readonly ConcurrentDictionary<string, JsonSchemaValidator?> _schemas = new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> s_outputAbsoluteUrlYamlMime = new(StringComparer.OrdinalIgnoreCase)
        {
            "Architecture",
            "TSType",
            "TSEnum",
        };

        private static readonly HashSet<string> s_yamlMimesMigratedFromMarkdown = new(StringComparer.OrdinalIgnoreCase)
        {
            "Architecture",
            "Hub",
            "Landing",
            "LandingData",
        };

        public TemplateEngine(ErrorBuilder errors, Config config, PackageResolver packageResolver, BuildOptions buildOptions, JsonSchemaLoader jsonSchemaLoader)
        {
            _config = config;
            _buildOptions = buildOptions;
            _jsonSchemaLoader = jsonSchemaLoader;

            var template = config.Template;
            var templateFetchOptions = PackageFetchOptions.DepthOne;
            if (template.Type == PackageType.None)
            {
                template = new("_themes");
                templateFetchOptions |= PackageFetchOptions.IgnoreDirectoryNonExistedError;
            }

            _package = packageResolver.ResolveAsPackage(template, templateFetchOptions);

            _templateDefinition = new(() => _package.TryLoadYamlOrJson<TemplateDefinition>(errors, "template") ?? new());

            _global = LoadGlobalTokens(errors, _package, _buildOptions.Locale);

            TemplateEngineRunner = new(_package, _global, config.TemplateBasePath);
        }

        public static JObject LoadGlobalTokens(ErrorBuilder errors, Package package, string locale)
        {
            var defaultTokens = package.TryLoadYamlOrJson<JObject>(errors, "ContentTemplate/token");
            var localeTokens = package.TryLoadYamlOrJson<JObject>(errors, $"ContentTemplate/token.{locale}");
            if (defaultTokens == null)
            {
                return localeTokens ?? new JObject();
            }
            JsonUtility.Merge(defaultTokens, localeTokens);
            return defaultTokens;
        }

        public RenderType GetRenderType(ContentType contentType, SourceInfo<string?> mime)
        {
            return contentType switch
            {
                ContentType.Redirection => RenderType.Content,
                ContentType.Page => GetRenderType(mime),
                ContentType.Toc => GetTocRenderType(),
                _ => RenderType.Component,
            };
        }

        public static bool OutputAbsoluteUrl(string? mime) => mime != null && s_outputAbsoluteUrlYamlMime.Contains(mime);

        public static bool IsConceptual(string? mime) => "Conceptual".Equals(mime, StringComparison.OrdinalIgnoreCase);

        public static bool IsLandingData(string? mime) => "LandingData".Equals(mime, StringComparison.OrdinalIgnoreCase);

        public static bool IsMigratedFromMarkdown(string? mime)
        {
            return mime != null && s_yamlMimesMigratedFromMarkdown.Contains(mime);
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

        public void CopyAssetsToOutput(Output output)
        {
            if (!_config.SelfContained || _templateDefinition.Value.Assets.Length <= 0)
            {
                return;
            }

            var glob = GlobUtility.CreateGlobMatcher(_templateDefinition.Value.Assets);

            Parallel.ForEach(_package.GetFiles(), file =>
            {
                if (glob(file))
                {
                    output.Copy(file, _package, file);
                }
            });
        }

        public string? GetToken(string key)
        {
            return _global[key]?.ToString();
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

        private JsonSchemaValidator? GetSchemaCore(string mime)
        {
            var jsonSchema = IsLandingData(mime)
                ? _jsonSchemaLoader.LoadSchema(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data/docs/landing-data.json")))
                : _jsonSchemaLoader.TryLoadSchema(_package, new PathString($"ContentTemplate/schemas/{mime}.schema.json"));

            if (jsonSchema is null)
            {
                return null;
            }

            return new JsonSchemaValidator(jsonSchema, forceError: true);
        }
    }
}
