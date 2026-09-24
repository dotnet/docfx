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
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Docfx.Build.RestApi;

internal static class OpenApiDocumentReader
{
    internal static RestApiRootItemViewModel Read(string path)
    {
        var format = Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) ? "json" : "yaml";
        var model = Parse(EnvironmentContext.FileAbstractLayer.ReadAllText(path), format, new Uri(Path.GetFullPath(path)));
        return model;
    }

    internal static RestApiRootItemViewModel Parse(string raw, string format, Uri baseUrl = null, string version = null)
    {
        try
        {
            version ??= RestApiDocumentReader.ReadHeader(new StringReader(raw), format)?.Version;
            var constants = new Dictionary<string, string>();
            var document = LoadDocuments(raw, format, baseUrl ?? new Uri(Path.GetFullPath("openapi.json")), version, constants);
            var model = new OpenApi3ModelConverter(document.BaseUri, constants).Convert(document, raw, version);
            model.Metadata["rawExtension"] = format == "json" ? ".json" : ".yaml";
            return model;
        }
        catch (Exception ex) when (ex is IOException or YamlException or JsonException or System.Text.Json.JsonException or OpenApiException or InvalidOperationException)
        {
            throw new DocfxException($"Unable to read OpenAPI document: {ex.Message}", ex);
        }
    }

    private static OpenApiDocument LoadDocuments(string raw, string format, Uri root, string rootVersion, Dictionary<string, string> constants)
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
            var version = location == root ? rootVersion : RestApiDocumentReader.ReadHeader(new StringReader(source), sourceFormat)?.Version;
            if (!System.Version.TryParse(version, out var parsed) || parsed.Major != 3 || parsed.Minor is not (0 or 1 or 2))
            {
                if (location == root)
                {
                    throw new DocfxException($"OpenAPI version '{version}' is not supported. Use OpenAPI 3.0, 3.1 or 3.2.");
                }
                throw new DocfxException($"UnsupportedExternalFragment: '{location.LocalPath}' is not a complete OpenAPI 3.0, 3.1 or 3.2 document. " +
                    "Standalone schema/component fragments are valid OpenAPI references, but are not supported by this reader integration.");
            }
            if (sourceFormat == "json")
            {
                // Replacing a const value must not make malformed JSON appear valid.
                using var json = System.Text.Json.JsonDocument.Parse(source);
            }
            source = PrepareSchemas(source, location, parsed.Minor == 0, constants);
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
            if (collector.HasEncoding || document.Tags?.Any(tag => tag.Parent != null || tag.Kind != null || tag.Summary != null) == true)
            {
                Logger.LogWarning($"OpenAPI '{location.LocalPath}': media-type encoding and tag summary, hierarchy and kind do not have dedicated documentation UI.");
            }
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

    private static string PrepareSchemas(string source, Uri location, bool openApi30, Dictionary<string, string> constants)
    {
        var replacements = new Dictionary<int, (int End, string Value)>();
        var yaml = new YamlStream();
        yaml.Load(new StringReader(source));
        // Resolve YAML aliases before editing source spans: an alias shares its
        // node's original span, which may belong to an example rather than a schema.
        if (yaml.Documents[0].AllNodes.Any(node => !node.Anchor.IsEmpty))
        {
            foreach (var node in yaml.Documents[0].AllNodes)
            {
                node.Anchor = AnchorName.Empty;
            }
            using var expanded = new StringWriter();
            yaml.Save(expanded, assignAnchors: false);
            source = expanded.ToString();
            yaml = new YamlStream();
            yaml.Load(new StringReader(source));
        }
        // Representation-model collection End marks describe the opening token.
        // Use parsing events to locate the end of a complete const object/array.
        var collectionEnds = new Dictionary<int, int>();
        var starts = new Stack<int>();
        var parser = new Parser(new StringReader(source));
        while (parser.MoveNext())
        {
            if (parser.Current is YamlDotNet.Core.Events.MappingStart or YamlDotNet.Core.Events.SequenceStart)
            {
                starts.Push((int)parser.Current.Start.Index);
            }
            else if (parser.Current is YamlDotNet.Core.Events.MappingEnd or YamlDotNet.Core.Events.SequenceEnd)
            {
                var end = (int)parser.Current.End.Index;
                if (parser.Current.Start.Index == end && end < source.Length && source[end] is '}' or ']')
                {
                    end++;
                }
                collectionEnds.Add(starts.Pop(), end);
            }
        }
        var root = yaml.Documents[0].RootNode;
        if (root is YamlMappingNode document &&
            document.Children.TryGetValue(new YamlScalarNode("components"), out var components) &&
            components is YamlMappingNode componentMap &&
            componentMap.Children.TryGetValue(new YamlScalarNode("schemas"), out var schemas))
        {
            CheckMap(schemas, "#/components/schemas");
        }
        VisitDocument(root, "#");
        var prepared = new StringBuilder(source);
        foreach (var (start, replacement) in replacements.OrderByDescending(pair => pair.Key))
        {
            prepared.Remove(start, replacement.End - start).Insert(start, replacement.Value);
        }
        return prepared.ToString();

        void Replace(YamlNode node, string value)
        {
            var start = (int)node.Start.Index;
            var end = collectionEnds.GetValueOrDefault(start, (int)node.End.Index);
            // Block collections/scalars can include the newline before the next field.
            var trimmedEnd = end;
            while (trimmedEnd > start && char.IsWhiteSpace(source[trimmedEnd - 1]))
            {
                trimmedEnd--;
            }
            replacements[start] = (end, value + source[trimmedEnd..end]);
        }

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
                if (name.StartsWith("x-", StringComparison.Ordinal) || name is "example" or "examples" or "default" or "enum" or "const" or "value" or "dataValue" or "serializedValue" or "schemas")
                {
                    continue;
                }
                if (name is "schema" or "itemSchema")
                {
                    CheckSchema(value, path + "/" + name);
                }
                else if (name == "$ref")
                {
                    CheckReference(value, path);
                }
                else if (value is YamlMappingNode entries && name is
                    ("paths" or "webhooks" or "responses" or "content" or "headers" or
                    "parameters" or "requestBodies" or "pathItems" or "callbacks" or "additionalOperations" or "mediaTypes"))
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
                    CheckSchema(value, path + "/" + key);
                }
            }
        }

        void CheckSchema(YamlNode node, string path)
        {
            // The SDK supports boolean schemas but its map/list readers drop scalars.
            if (node is YamlScalarNode { Style: ScalarStyle.Plain, Value: { } boolean } &&
                bool.TryParse(boolean, out var allowed))
            {
                if (openApi30)
                {
                    throw new DocfxException($"InvalidOpenApiSchema: boolean schema at '{path}' in '{location.LocalPath}' requires OpenAPI 3.1 or 3.2.");
                }
                Replace(node, allowed ? "{}" : "{\"not\":{}}");
                return;
            }
            if (node is not YamlMappingNode schema)
            {
                return;
            }
            foreach (var (key, value) in schema.Children)
            {
                var name = ((YamlScalarNode)key).Value;
                switch (name)
                {
                    case "const" or "default" when value is YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" } scalar && scalar.Tag != "tag:yaml.org,2002:str":
                        if (name == "const" && !openApi30)
                        {
                            PreserveConst(value);
                        }
                        else
                        {
                            Replace(value, " null");
                        }
                        break;
                    case "const" when !openApi30:
                        PreserveConst(value);
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

        void PreserveConst(YamlNode node)
        {
            // OpenAPI.NET 3.10.2 models Const as string. Carry an opaque token through
            // its reference resolution and restore the JSON value during conversion.
            var token = Guid.NewGuid().ToString("N");
            constants.Add(token, JsonLiteral(node));
            Replace(node, " " + JsonConvert.SerializeObject(token));
        }

        static string JsonLiteral(YamlNode node)
        {
            if (node is YamlMappingNode map)
            {
                return "{" + string.Join(",", map.Children.Select(pair =>
                    JsonConvert.SerializeObject(((YamlScalarNode)pair.Key).Value) + ":" + JsonLiteral(pair.Value))) + "}";
            }
            if (node is YamlSequenceNode sequence)
            {
                return "[" + string.Join(",", sequence.Children.Select(JsonLiteral)) + "]";
            }
            var scalar = (YamlScalarNode)node;
            var value = scalar.Value;
            if (scalar.Style == ScalarStyle.Plain && scalar.Tag != "tag:yaml.org,2002:str")
            {
                if (string.IsNullOrEmpty(value) || value == "~" || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return "null";
                }
                if (bool.TryParse(value, out var boolean))
                {
                    return boolean ? "true" : "false";
                }
                // Preserve JSON numbers lexically, including large integers/exponents.
                if (System.Text.RegularExpressions.Regex.IsMatch(value, @"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$"))
                {
                    return value;
                }
                if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return number.ToString(CultureInfo.InvariantCulture);
                }
            }
            return JsonConvert.SerializeObject(value ?? "");
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
        internal bool HasEncoding { get; private set; }
        internal List<(IOpenApiReferenceHolder Holder, BaseOpenApiReference Reference)> References { get; } = [];
        private readonly HashSet<IOpenApiReferenceHolder> _visitedReferences = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<IOpenApiSchema> _visitedSchemas = new(ReferenceEqualityComparer.Instance);

        public override void Visit(IOpenApiMediaType media)
        {
            HasEncoding |= media.Encoding is { Count: > 0 } || media.ItemEncoding != null || media.PrefixEncoding is { Count: > 0 };
            // OpenAPI.NET 3.10.2's walker visits Schema but omits ItemSchema.
            if (media.ItemSchema != null)
            {
                WalkSchema(media.ItemSchema);
            }
        }

        public override void Visit(IOpenApiReferenceHolder holder)
        {
            // Operation tags are names, not required references to root tags.
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
                WalkSchema(OpenApi3ModelConverter.GetReferenceSiblings(schemaReference));
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
