// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Docfx.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Docfx.Plugins;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Docfx.Build.RestApi;

internal static class OpenApiDocumentReader
{
    internal static bool IsOpenApiFile(string path)
    {
        try
        {
            return GetVersion(EnvironmentContext.FileAbstractLayer.ReadAllText(path)) != null;
        }
        catch (FileNotFoundException ex)
        {
            Logger.LogVerbose($"Could not find OpenAPI file '{path}': {ex.Message}");
        }
        catch (DirectoryNotFoundException ex)
        {
            Logger.LogVerbose($"Could not find OpenAPI file '{path}': {ex.Message}");
        }
        catch (YamlException ex)
        {
            Logger.LogVerbose($"Could not read OpenAPI version in '{path}': {ex.Message}");
        }
        return false;
    }

    internal static RestApiRootItemViewModel Read(string path)
    {
        var format = Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) ? "json" : "yaml";
        var model = Parse(EnvironmentContext.FileAbstractLayer.ReadAllText(path), format, new Uri(Path.GetFullPath(path)));
        return model;
    }

    internal static RestApiRootItemViewModel Parse(string raw, string format, Uri baseUrl = null)
    {
        try
        {
            var version = GetVersion(raw);
            if (!System.Version.TryParse(version, out var parsed) || parsed.Major != 3 || parsed.Minor is not (0 or 1))
            {
                throw new DocfxException($"OpenAPI version '{version}' is not supported. Use OpenAPI 3.0 or 3.1.");
            }
            var document = LoadDocuments(raw, format, baseUrl ?? new Uri(Path.GetFullPath("openapi.json")));
            var model = new OpenApiModelConverter(document.BaseUri).Convert(document, raw, version);
            model.Metadata["rawExtension"] = format == "json" ? ".json" : ".yaml";
            return model;
        }
        catch (Exception ex) when (ex is IOException or YamlException or System.Text.Json.JsonException or OpenApiException or InvalidOperationException)
        {
            throw new DocfxException($"Unable to read OpenAPI document: {ex.Message}", ex);
        }
    }

    private static OpenApiDocument LoadDocuments(string raw, string format, Uri root)
    {
        var loader = new LocalStreamLoader();
        var documents = new Dictionary<Uri, OpenApiDocument>();
        var references = new Dictionary<Uri, List<(IOpenApiReferenceHolder Holder, BaseOpenApiReference Reference)>>();
        var pending = new Queue<Uri>();
        var scheduled = new HashSet<Uri> { root };
        pending.Enqueue(root);
        while (pending.TryDequeue(out var location))
        {
            var source = raw;
            var sourceFormat = format;
            if (location != root)
            {
                using var input = loader.LoadAsync(root, location).GetAwaiter().GetResult();
                using var reader = new StreamReader(input);
                source = reader.ReadToEnd();
                sourceFormat = Path.GetExtension(location.LocalPath).Equals(".json", StringComparison.OrdinalIgnoreCase) ? "json" : "yaml";
            }
            var version = GetVersion(source);
            if (!System.Version.TryParse(version, out var parsed) || parsed.Major != 3 || parsed.Minor is not (0 or 1))
            {
                throw new DocfxException($"UnsupportedExternalFragment: '{location.LocalPath}' is not a complete OpenAPI 3.0 or 3.1 document. " +
                    "Standalone schema/component fragments are valid OpenAPI references, but are not supported by this reader integration.");
            }
            CheckSchemaReaderLimitations(source, location, parsed.Minor == 0);
            var settings = new OpenApiReaderSettings
            {
                BaseUrl = location,
                LoadExternalRefs = false,
                CustomExternalLoader = loader
            };
            settings.AddYamlReader();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(source));
            var result = Task.Run(() => OpenApiDocument.LoadAsync(stream, sourceFormat, settings)).GetAwaiter().GetResult();
            if (result.Diagnostic.Errors.Count > 0)
            {
                throw new DocfxException($"Invalid OpenAPI document '{location.LocalPath}': " +
                    string.Join("; ", result.Diagnostic.Errors.Select(e => e.ToString())));
            }
            foreach (var warning in result.Diagnostic.Warnings)
            {
                Logger.LogWarning($"OpenAPI '{location.LocalPath}': {warning}");
            }
            var document = result.Document ?? throw new DocfxException($"The OpenAPI reader did not produce a document for '{location.LocalPath}'.");
            if (document.Components?.Schemas?.Any(pair => pair.Value == null) == true)
            {
                throw new DocfxException($"UnsupportedBooleanSchema: OpenAPI.NET could not read a component schema in '{location.LocalPath}'.");
            }
            documents.Add(location, document);
            if (document.Webhooks is { Count: > 0 } || document.Security is { Count: > 0 } ||
                document.Components?.SecuritySchemes is { Count: > 0 } ||
                document.Paths?.Values.Any(path => path.Operations?.Values.Any(operation =>
                    operation.Callbacks is { Count: > 0 } || operation.Security is { Count: > 0 } ||
                    operation.Responses?.Values.Any(response => response.Links is { Count: > 0 }) == true) == true) == true)
            {
                Logger.LogWarning($"OpenAPI '{location.LocalPath}': callbacks, webhooks, security configuration and response links do not have dedicated documentation UI.");
            }
            var collector = new ReferenceCollector();
            new OpenApiWalker(collector).Walk(document);
            references.Add(location, collector.References);
            foreach (var (_, reference) in collector.References)
            {
                if (reference.ExternalResource is { } external)
                {
                    var target = LocalStreamLoader.Resolve(location, new Uri(external, UriKind.RelativeOrAbsolute));
                    if (scheduled.Add(target))
                    {
                        pending.Enqueue(target);
                    }
                }
            }
        }

        // The SDK aliases external names globally within a workspace. Each host needs its own
        // aliases so two documents can both refer to "common.yaml" in different directories.
        foreach (var (location, document) in documents)
        {
            document.Workspace = new OpenApiWorkspace();
            foreach (var other in documents.Values)
            {
                document.Workspace.RegisterComponents(other);
            }
            foreach (var (_, reference) in references[location])
            {
                if (reference.ExternalResource is { } external)
                {
                    document.Workspace.AddDocumentId(external, new Uri(location, external));
                }
            }
        }
        foreach (var (location, holders) in references)
        {
            foreach (var (holder, reference) in holders)
            {
                if (holder.UnresolvedReference)
                {
                    throw new DocfxException($"Could not resolve OpenAPI reference '{reference.ReferenceV3}' in '{location.LocalPath}'.");
                }
            }
        }
        return documents[root];
    }

    private static void CheckSchemaReaderLimitations(string source, Uri location, bool openApi30)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(source));
        var root = yaml.Documents[0].RootNode;
        if (root is YamlMappingNode document &&
            document.Children.TryGetValue(new YamlScalarNode("components"), out var components) &&
            components is YamlMappingNode componentMap &&
            componentMap.Children.TryGetValue(new YamlScalarNode("schemas"), out var schemas))
        {
            CheckMap(schemas, "#/components/schemas");
        }
        VisitDocument(root, "#");

        void VisitDocument(YamlNode node, string path)
        {
            if (node is YamlSequenceNode sequence)
            {
                for (var i = 0; i < sequence.Children.Count; i++)
                {
                    VisitDocument(sequence.Children[i], path + "/" + i);
                }
            }
            if (node is not YamlMappingNode mapping)
            {
                return;
            }
            foreach (var (key, value) in mapping.Children)
            {
                var name = ((YamlScalarNode)key).Value;
                if (name.StartsWith("x-", StringComparison.Ordinal) || name is "example" or "examples" or "default" or "enum" or "const" or "value" or "schemas")
                {
                    continue;
                }
                if (name == "schema")
                {
                    CheckSchema(value, path + "/schema");
                }
                else if (name == "$ref")
                {
                    CheckReference(value, path);
                }
                else if (value is YamlMappingNode entries && name is
                    ("paths" or "webhooks" or "responses" or "content" or "headers" or
                    "parameters" or "requestBodies" or "pathItems" or "callbacks"))
                {
                    // Map keys are names, not object fields: a "default" response or
                    // a parameter named "schema" still contains a schema. Only Paths
                    // and Responses Objects allow extensions alongside these entries.
                    foreach (var (entryKey, entryValue) in entries.Children)
                    {
                        if (path != "#/components" && name is ("paths" or "responses") &&
                            ((YamlScalarNode)entryKey).Value.StartsWith("x-", StringComparison.Ordinal))
                        {
                            continue;
                        }
                        VisitDocument(entryValue, path + "/" + name + "/" + entryKey);
                    }
                }
                else
                {
                    VisitDocument(value, path + "/" + name);
                }
            }
        }

        void CheckMap(YamlNode node, string path)
        {
            if (node is YamlMappingNode map)
            {
                foreach (var (key, value) in map.Children)
                {
                    RejectBoolean(value, path + "/" + key);
                    CheckSchema(value, path + "/" + key);
                }
            }
        }

        void CheckSchema(YamlNode node, string path)
        {
            if (node is not YamlMappingNode schema)
            {
                return;
            }
            foreach (var (key, value) in schema.Children)
            {
                var name = ((YamlScalarNode)key).Value;
                switch (name)
                {
                    case "const" or "default" when value is YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" }:
                        throw new DocfxException($"UnsupportedOpenApiNullValue: OpenAPI.NET 3.10.2 reads the implicit YAML null at '{path}/{name}' in '{location.LocalPath}' as an empty string. " +
                            $"Write '{name}: null' explicitly to preserve its meaning.");
                    case "const" when !openApi30:
                        RejectLossyConst(value, path + "/const");
                        break;
                    case "$ref":
                        CheckReference(value, path);
                        break;
                    case "$dynamicRef":
                        throw new DocfxException($"UnsupportedOpenApiSchema: dynamic references at '{path}' in '{location.LocalPath}' are not supported.");
                    case "properties" or "patternProperties" or "$defs" or "dependentSchemas":
                        CheckMap(value, path + "/" + name);
                        break;
                    case "allOf" or "oneOf" or "anyOf":
                        if (value is YamlSequenceNode sequence)
                        {
                            CheckPrimitiveUnion(schema, sequence, name, path);
                            for (var i = 0; i < sequence.Children.Count; i++)
                            {
                                RejectBoolean(sequence.Children[i], path + "/" + name + "/" + i);
                                CheckSchema(sequence.Children[i], path + "/" + name + "/" + i);
                            }
                        }
                        break;
                    case "items" or "not" or "additionalProperties" or "unevaluatedProperties" or "contains" or "propertyNames" or "if" or "then" or "else" or "contentSchema":
                        CheckSchema(value, path + "/" + name);
                        break;
                }
            }
        }

        void RejectLossyConst(YamlNode node, string path)
        {
            // OpenAPI.NET 3.10.2 reads const with GetScalarValue, turning numbers and
            // booleans into strings and rejecting objects/arrays. Quoted scalars and
            // explicit null remain supported; never infer a constant's type from type.
            if (node is YamlMappingNode or YamlSequenceNode ||
                node is YamlScalarNode { Style: ScalarStyle.Plain, Value: { } value } &&
                (bool.TryParse(value, out _) ||
                    (value.Any(char.IsAsciiDigit) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))))
            {
                throw new DocfxException($"UnsupportedOpenApiConst: OpenAPI.NET 3.10.2 cannot preserve the const value at '{path}' in '{location.LocalPath}'. " +
                    "Only string and null const values are supported.");
            }
        }

        void RejectBoolean(YamlNode node, string path)
        {
            // OpenAPI.NET 3.10.2 JsonNodeHelper.CreateMap/CreateList drop non-object schemas.
            // Do not rewrite them: fail before the SDK can silently change their meaning.
            if (node is YamlScalarNode { Style: ScalarStyle.Plain, Value: { } value } &&
                (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase)))
            {
                throw new DocfxException($"UnsupportedBooleanSchema: OpenAPI.NET 3.10.2 cannot preserve the boolean schema at '{path}' in '{location.LocalPath}'.");
            }
        }

        void CheckReference(YamlNode node, string path)
        {
            if (node is YamlScalarNode { Value: { } value } && !value.Contains('#'))
            {
                throw new DocfxException($"UnsupportedExternalFragment: reference '{value}' at '{path}' in '{location.LocalPath}' " +
                    "requires a complete OpenAPI component document and a fragment identifier.");
            }
        }

        void CheckPrimitiveUnion(YamlMappingNode schema, YamlSequenceNode sequence, string kind, string path)
        {
            if (!openApi30 || kind == "allOf" || schema.Children.ContainsKey(new YamlScalarNode("type")) || sequence.Children.Count == 0)
            {
                return;
            }
            var types = new List<string>();
            var hasExamples = false;
            foreach (var node in sequence.Children)
            {
                if (node is not YamlMappingNode branch ||
                    branch.Children.Keys.Any(key => ((YamlScalarNode)key).Value is not ("type" or "example" or "examples")) ||
                    !branch.Children.TryGetValue(new YamlScalarNode("type"), out var type) ||
                    type is not YamlScalarNode { Value: "string" or "integer" or "number" or "boolean" or "object" or "array" or "null" } scalar)
                {
                    return;
                }
                types.Add(scalar.Value);
                hasExamples |= branch.Children.Count > 1;
            }
            if (hasExamples || (kind == "oneOf" && (types.Distinct().Count() != types.Count ||
                (types.Contains("integer") && types.Contains("number")))))
            {
                throw new DocfxException($"UnsupportedOpenApiComposition: OpenAPI.NET 3.10.2 would lose exclusive alternatives or branch examples " +
                    $"in '{kind}' at '{path}' in '{location.LocalPath}'.");
            }
        }
    }

    private sealed class ReferenceCollector : OpenApiVisitorBase
    {
        internal List<(IOpenApiReferenceHolder Holder, BaseOpenApiReference Reference)> References { get; } = [];
        private readonly HashSet<IOpenApiReferenceHolder> _visitedReferences = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<IOpenApiSchema> _visitedSchemas = new(ReferenceEqualityComparer.Instance);

        public override void Visit(IOpenApiReferenceHolder holder)
        {
            // Operation tags in OpenAPI 3.0/3.1 are names, not required references to root tags.
            if (holder is OpenApiTagReference)
            {
                return;
            }
            if (!_visitedReferences.Add(holder))
            {
                return;
            }
            BaseOpenApiReference reference = holder switch
            {
                IOpenApiReferenceHolder<JsonSchemaReference> schema => schema.Reference,
                IOpenApiReferenceHolder<OpenApiReferenceWithDescriptionAndSummary> summarized => summarized.Reference,
                IOpenApiReferenceHolder<OpenApiReferenceWithDescription> described => described.Reference,
                IOpenApiReferenceHolder<BaseOpenApiReference> basic => basic.Reference,
                _ => throw new DocfxException($"Unsupported OpenAPI reference holder '{holder.GetType().Name}'.")
            };
            References.Add((holder, reference));
            if (holder is OpenApiSchemaReference schemaReference)
            {
                WalkSchema(OpenApiModelConverter.GetReferenceSiblings(schemaReference));
            }
        }

        public override void Visit(IOpenApiSchema schema)
        {
            if (!_visitedSchemas.Add(schema))
            {
                return;
            }
            foreach (var child in (schema.Definitions?.Values ?? Enumerable.Empty<IOpenApiSchema>())
                .Concat(schema.PatternProperties?.Values ?? Enumerable.Empty<IOpenApiSchema>()))
            {
                WalkSchema(child);
            }
            if (schema is IOpenApiSchemaMissingProperties extra)
            {
                foreach (var child in new[] { extra.If, extra.Then, extra.Else, extra.Contains, extra.ContentSchema, extra.PropertyNames, extra.UnevaluatedPropertiesSchema }
                    .Concat(extra.DependentSchemas?.Values ?? Enumerable.Empty<IOpenApiSchema>()).Where(s => s != null))
                {
                    WalkSchema(child);
                }
            }
        }

        private void WalkSchema(IOpenApiSchema schema) => new OpenApiWalker(this).Walk(new OpenApiDocument
        {
            Components = new OpenApiComponents { Schemas = new Dictionary<string, IOpenApiSchema> { ["schema"] = schema } }
        });
    }

    private static string GetVersion(string raw)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(raw));
        return yaml.Documents.Count == 1 &&
            yaml.Documents[0].RootNode is YamlMappingNode root &&
            root.Children.TryGetValue(new YamlScalarNode("openapi"), out var node) &&
            node is YamlScalarNode version ? version.Value : null;
    }

    private sealed class LocalStreamLoader : IStreamLoader
    {
        public Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(EnvironmentContext.FileAbstractLayer.OpenRead(Resolve(baseUrl, uri).LocalPath));
        }

        internal static Uri Resolve(Uri baseUrl, Uri uri)
        {
            uri = uri.IsAbsoluteUri ? uri : new Uri(baseUrl, uri);
            if (!uri.IsAbsoluteUri || !uri.IsFile || uri.IsUnc || !string.IsNullOrEmpty(uri.Host))
            {
                throw new DocfxException($"Only local file references are supported in OpenAPI documents: '{uri}'.");
            }
            return new Uri(Path.GetFullPath(uri.LocalPath));
        }
    }
}
