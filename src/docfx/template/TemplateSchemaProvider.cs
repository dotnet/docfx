// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateSchemaProvider
    {
        private readonly PackagePath _template;
        private readonly Package _package;
        private readonly JObject _global;
        private readonly JsonSchemaLoader _jsonSchemaLoader;

        private readonly ConcurrentDictionary<string, JsonSchemaValidator?> _schemas = new(StringComparer.OrdinalIgnoreCase);

        public TemplateSchemaProvider(PackagePath template, Package package, JsonSchemaLoader jsonSchemaLoader, JObject global)
        {
            _template = template;
            _jsonSchemaLoader = jsonSchemaLoader;
            _package = package;
            _global = global;
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

        public JsonSchemaValidator GetSchemaValidator(SourceInfo<string?> mime)
        {
            var name = mime.Value ?? throw Errors.Yaml.SchemaNotFound(mime).ToException();

            return _schemas.GetOrAdd(name, GetSchemaCore) ?? throw Errors.Yaml.SchemaNotFound(mime).ToException();
        }

        public string? GetToken(string key)
        {
            return _global[key]?.ToString();
        }

        private JsonSchema GetSchema(SourceInfo<string?> mime)
        {
            return GetSchemaValidator(mime).Schema;
        }

        private JsonSchemaValidator? GetSchemaCore(string mime)
        {
            var jsonSchema = TemplateEngineUtility.IsLandingData(mime)
                ? _jsonSchemaLoader.LoadSchema(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data/docs/landing-data.json")))
                : _jsonSchemaLoader.TryLoadSchema(_package, new PathString($"ContentTemplate/schemas/{mime}.schema.json"));

            if (jsonSchema is null)
            {
                return null;
            }

            return new JsonSchemaValidator(jsonSchema, forceError: true);
        }

        private RenderType GetRenderType(SourceInfo<string?> mime)
        {
            if (mime == null || TemplateEngineUtility.IsConceptual(mime) || TemplateEngineUtility.IsLandingData(mime))
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
                var isContentRenderType = _template.Url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    || _template.Path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                return isContentRenderType ? RenderType.Content : RenderType.Component;
            }
        }
    }
}
