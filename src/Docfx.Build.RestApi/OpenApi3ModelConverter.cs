// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi;

internal sealed partial class OpenApi3ModelConverter(IReadOnlyDictionary<string, string> constants)
{
    internal RestApiRootItemViewModel Convert(OpenApiDocument document, string raw, string version)
    {
        var servers = Servers(document.Servers);
        var server = (string)servers[0]["url"];
        var absolute = Uri.TryCreate(server, UriKind.Absolute, out var uri) && !uri.IsFile;
        var uid = GenerateUid(absolute ? uri.Authority : null, (absolute ? uri.AbsolutePath : server).Trim('/'),
            document.Info.Title, document.Info.Version);
        var model = new RestApiRootItemViewModel
        {
            Uid = uid,
            HtmlId = GetHtmlId(uid),
            Name = document.Info.Title,
            Description = document.Info.Description,
            Summary = document.Info.Summary,
            Raw = raw,
            Metadata = Extensions(document.Extensions),
            Children = [],
            Tags = []
        };
        model.Metadata["specificationVersion"] = version;
        model.Metadata["servers"] = servers;
        model.Metadata["info"] = Serialize(document.Info);
        if (document.ExternalDocs != null)
        {
            model.Metadata["externalDocs"] = Serialize(document.ExternalDocs);
        }
        var schemas = new JObject();
        foreach (var (name, schema) in document.Components?.Schemas?.AsEnumerable() ?? [])
        {
            schemas[name] = Schema(schema);
        }
        model.Metadata["schemas"] = schemas;
        foreach (var tag in document.Tags?.AsEnumerable() ?? [])
        {
            AddTag(tag.Name, tag.Description, Extensions(tag.Extensions));
        }

        var operationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (path, pathItem) in document.Paths ?? [])
        {
            foreach (var (method, operation) in pathItem.Operations ?? [])
            {
                var methodName = method.ToString().ToLowerInvariant();
                var id = string.IsNullOrEmpty(operation.OperationId)
                    ? methodName + "_" + System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path))).ToLowerInvariant()
                    : operation.OperationId;
                if (!operationIds.Add(id))
                {
                    throw new DocfxException($"OpenAPI operation ID '{id}' is not unique.");
                }
                var operationUid = GenerateUid(uid, id);
                var effectiveServers = Servers(operation.Servers is { Count: > 0 } ? operation.Servers :
                    pathItem.Servers is { Count: > 0 } ? pathItem.Servers : document.Servers);
                var parameters = MergeParameters(operation.Parameters, pathItem.Parameters);
                var child = new RestApiChildItemViewModel
                {
                    Uid = operationUid,
                    HtmlId = GetHtmlId(operationUid),
                    OperationId = id,
                    OperationName = methodName,
                    Path = path,
                    Summary = operation.Summary,
                    Description = operation.Description,
                    Tags = operation.Tags?.Select(t => t.Name).ToList() ?? [],
                    Parameters = parameters?.Select(Parameter).ToList() ?? [],
                    Responses = operation.Responses?.Select(pair => Response(pair.Key, pair.Value)).ToList() ?? [],
                    Metadata = Extensions(operation.Extensions)
                };
                child.Metadata["servers"] = effectiveServers;
                child.Metadata["requestUrl"] = ((string)effectiveServers[0]["url"]).TrimEnd('/') + "/" + path.TrimStart('/');
                if (operation.RequestBody is { } body)
                {
                    child.Metadata["requestBody"] = new JObject
                    {
                        ["description"] = body.Description,
                        ["required"] = body.Required,
                        ["content"] = Content(body.Content)
                    };
                }
                foreach (var name in child.Tags)
                {
                    if (!model.Tags.Any(t => t.Name == name))
                    {
                        AddTag(name, null, []);
                    }
                }
                model.Children.Add(child);
            }
        }

        return model;

        void AddTag(string name, string description, Dictionary<string, object> metadata)
        {
            if (model.Tags.Any(tag => tag.Name == name))
            {
                return;
            }
            model.Tags.Add(new RestApiTagViewModel
            {
                Name = name,
                Description = description,
                Uid = GenerateUid(uid, "tag", name),
                HtmlId = metadata.TryGetValue("x-bookmark-id", out var bookmark) ? bookmark?.ToString() : GetHtmlId(name),
                Metadata = metadata
            });
        }
    }

    private static JArray Servers(IList<OpenApiServer> servers)
    {
        if (servers == null || servers.Count == 0)
        {
            return new JArray(new JObject { ["url"] = "/" });
        }
        return new JArray(servers.Select(server =>
        {
            var url = server.Url;
            foreach (var (name, variable) in server.Variables?.AsEnumerable() ?? [])
            {
                if (variable.Default == null)
                {
                    throw new DocfxException($"OpenAPI server variable '{name}' requires a default.");
                }
                url = url.Replace("{" + name + "}", variable.Default, StringComparison.Ordinal);
            }
            if (url.Contains('{') || url.Contains('}'))
            {
                throw new DocfxException($"OpenAPI server URL '{server.Url}' contains a variable without a default.");
            }
            return new JObject { ["url"] = url, ["description"] = server.Description };
        }));
    }

    private RestApiParameterViewModel Parameter(IOpenApiParameter parameter)
    {
        var schema = Schema(parameter.Schema);
        var metadata = Extensions(parameter.Extensions);
        metadata["in"] = parameter.In?.ToString().ToLowerInvariant();
        metadata["required"] = parameter.Required;
        metadata["style"] = parameter.Style?.ToString();
        metadata["explode"] = parameter.Explode;
        metadata["schema"] = schema;
        if (parameter.Content is { Count: > 0 })
        {
            metadata["content"] = Content(parameter.Content);
        }
        if (parameter.Schema?.Default != null)
        {
            metadata["default"] = Literal(parameter.Schema.Default);
        }
        return new RestApiParameterViewModel
        {
            Name = parameter.Name,
            Description = parameter.Description,
            Metadata = metadata
        };
    }

    private RestApiResponseViewModel Response(string status, IOpenApiResponse response)
    {
        var metadata = Extensions(response.Extensions);
        metadata["content"] = Content(response.Content);
        return new RestApiResponseViewModel
        {
            HttpStatusCode = status,
            Description = response.Description,
            Metadata = metadata
        };
    }

    private JArray Content(IDictionary<string, IOpenApiMediaType> content) =>
        new(content?.Select(pair => new JObject
        {
            ["mimeType"] = pair.Key,
            ["schema"] = Schema(pair.Value.Schema),
            ["itemSchema"] = Schema(pair.Value.ItemSchema),
            ["examples"] = Examples(pair.Key, pair.Value)
        }) ?? []);

    private static JArray Examples(string mimeType, IOpenApiMediaType media)
    {
        var result = new JArray();
        if (media.Example != null)
        {
            result.Add(new JObject { ["mimeType"] = mimeType, ["content"] = Literal(media.Example) });
        }
        foreach (var (name, example) in media.Examples?.AsEnumerable() ?? [])
        {
            result.Add(new JObject
            {
                ["name"] = name,
                ["mimeType"] = mimeType,
                ["content"] = example.SerializedValue ?? Literal(example.DataValue ?? example.Value),
                ["externalValue"] = example.ExternalValue
            });
        }
        return result;
    }

    private static string Literal(JsonNode value)
    {
        if (value == null)
        {
            return null;
        }
        // The YAML reader uses a sentinel for nulls, including nested values.
        // The SDK writer restores them; JsonNode.ToJsonString exposes the sentinel.
        using var text = new StringWriter();
        new OpenApiJsonWriter(text).WriteAny(value);
        return text.ToString();
    }

    private static JToken Serialize(IOpenApiSerializable value)
    {
        using var text = new StringWriter();
        value.SerializeAsV32(new OpenApiJsonWriter(text));
        return JToken.Parse(text.ToString());
    }

    private static Dictionary<string, object> Extensions(IDictionary<string, IOpenApiExtension> extensions)
    {
        var result = new Dictionary<string, object>();
        foreach (var (name, extension) in extensions?.AsEnumerable() ?? [])
        {
            using var text = new StringWriter();
            extension.Write(new OpenApiJsonWriter(text), OpenApiSpecVersion.OpenApi3_2);
            var token = JToken.Parse(text.ToString());
            result[name] = token is JValue value ? value.Value : token;
        }
        return result;
    }

    private JObject Schema(IOpenApiSchema schema, HashSet<IOpenApiSchema> ancestors = null)
    {
        if (schema == null)
        {
            return null;
        }
        ancestors ??= new(ReferenceEqualityComparer.Instance);
        if (schema is OpenApiSchemaReference reference)
        {
            var target = reference.Target ?? throw new DocfxException($"Could not resolve OpenAPI schema reference '{reference.Reference?.Id}'.");
            if (ancestors.Contains(target) || !ancestors.Add(reference))
            {
                if (target is OpenApiSchemaReference)
                {
                    throw new DocfxException($"Cyclic OpenAPI schema alias '{reference.Reference?.Id}' has no concrete schema.");
                }
                return new JObject { ["type"] = "recursive reference", ["x-internal-loop-ref-name"] = ReferenceName(reference) };
            }
            try
            {
                var targetModel = Schema(target, ancestors);
                var siblings = GetReferenceSiblings(reference);
                using var siblingText = new StringWriter();
                siblings.SerializeAsV32(new OpenApiJsonWriter(siblingText));
                var result = JObject.Parse(siblingText.ToString()).Count == 0 ? targetModel : new JObject
                {
                    ["type"] = "all of",
                    ["description"] = reference.Reference.Description ?? target.Description,
                    ["allOf"] = new JArray(targetModel, Schema(siblings, ancestors))
                };
                result["x-internal-ref-name"] ??= ReferenceName(reference);
                return result;
            }
            finally
            {
                ancestors.Remove(reference);
            }
        }
        if (!ancestors.Add(schema))
        {
            return new JObject { ["type"] = "recursive reference", ["x-internal-loop-ref-name"] = schema.Title ?? "schema" };
        }

        try
        {
            using var text = new StringWriter();
            schema.SerializeAsV32(new OpenApiJsonWriter(text));
            var serialized = JToken.Parse(text.ToString());
            RestoreConstants(serialized);
            if (serialized is JObject { Count: 1 } && serialized["not"] is JObject { Count: 0 })
            {
                return new JObject { ["type"] = "no value" };
            }

            var result = JObject.FromObject(Extensions(schema.Extensions));
            result["type"] = schema.Type?.ToString().ToLowerInvariant().Replace(", ", " | ") ??
                (serialized is JObject { Count: 0 } ? "any value" : "any type");
            if (schema.Format != null) result["format"] = schema.Format;
            if (schema.Description != null) result["description"] = schema.Description;
            if (schema.Properties != null)
            {
                result["properties"] = new JObject(schema.Properties.Select(pair =>
                {
                    var property = Schema(pair.Value, ancestors);
                    if (schema.Required?.Contains(pair.Key) == true)
                    {
                        property["required"] = true;
                    }
                    return new JProperty(pair.Key, property);
                }));
            }
            if (schema.Items != null) result["items"] = Schema(schema.Items, ancestors);
            var composition = new JArray();
            if (schema.AllOf is { Count: > 0 }) result["allOf"] = new JArray(schema.AllOf.Select(s => Schema(s, ancestors)));
            AddComposition("One of", schema.OneOf);
            AddComposition("Any of", schema.AnyOf);
            if (schema.Not != null)
            {
                AddComposition("Not", [schema.Not]);
            }
            if (composition.Count > 0)
            {
                result["composition"] = composition;
            }
            var constraints = new JArray();
            foreach (var property in ((JObject)serialized).Properties())
            {
                if (!property.Name.StartsWith("x-", StringComparison.Ordinal) && property.Name is not
                    ("type" or "format" or "description" or "properties" or "items" or "allOf" or "oneOf" or "anyOf" or "not" or
                    "additionalProperties" or "enum" or "example" or "examples"))
                {
                    constraints.Add(new JObject { ["name"] = property.Name, ["value"] = property.Value.ToString(Formatting.None) });
                }
            }
            if (schema.AdditionalProperties != null)
            {
                composition.Add(new JObject { ["kind"] = "Additional properties", ["schemas"] = new JArray(Schema(schema.AdditionalProperties, ancestors)) });
                result["composition"] = composition;
            }
            else if (!schema.AdditionalPropertiesAllowed)
            {
                constraints.Add(new JObject { ["name"] = "additionalProperties", ["value"] = "false" });
            }
            if (constraints.Count > 0)
            {
                result["constraints"] = constraints;
            }
            if (schema.Enum is { Count: > 0 })
            {
                result["enum"] = new JArray(schema.Enum.Select(value => value == null ? null : JToken.Parse(Literal(value))));
            }
            if (schema.Examples is { Count: > 0 })
            {
                result["examples"] = new JArray(schema.Examples.Select(example => new JObject { ["content"] = Literal(example) }));
            }
#pragma warning disable CS0618 // OpenAPI 3.0's singular schema example is still read into this SDK property.
            else if (schema.Example != null)
            {
                result["examples"] = new JArray(new JObject { ["content"] = Literal(schema.Example) });
            }
#pragma warning restore CS0618
            return result;

            void AddComposition(string kind, IList<IOpenApiSchema> schemas)
            {
                if (schemas is { Count: > 0 })
                {
                    composition.Add(new JObject { ["kind"] = kind, ["schemas"] = new JArray(schemas.Select(s => Schema(s, ancestors))) });
                }
            }
        }
        finally
        {
            ancestors.Remove(schema);
        }
    }

    private void RestoreConstants(JToken node)
    {
        if (node is JObject obj && obj["const"] is JValue { Type: JTokenType.String } value &&
            constants.TryGetValue((string)value, out var literal))
        {
            obj["const"] = new JRaw(literal);
        }
        foreach (var child in node.Children())
        {
            RestoreConstants(child);
        }
    }

    internal static OpenApiSchema GetReferenceSiblings(OpenApiSchemaReference reference)
    {
        var detached = new OpenApiSchemaReference(reference.Reference.Id)
        {
            Reference = new JsonSchemaReference(reference.Reference) { HostDocument = null }
        };
        var siblings = (OpenApiSchema)detached.CopyReferenceAsTargetElementWithOverrides(new OpenApiSchema());
        siblings.Description = reference.Reference.Description;
        return siblings;
    }

    private static string ReferenceName(OpenApiSchemaReference reference) => reference.Reference.Id;

    [GeneratedRegex(@"\W")]
    private static partial Regex HtmlEncodeRegex();

    private static string GetHtmlId(string id) => string.IsNullOrEmpty(id) ? null : HtmlEncodeRegex().Replace(id, "_");

    private static string GenerateUid(params string[] segments) =>
        string.Join('/', segments.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim('/')));

    private static IEnumerable<IOpenApiParameter> MergeParameters(IList<IOpenApiParameter> operationParameters, IList<IOpenApiParameter> pathParameters)
    {
        return (operationParameters ?? []).Concat((pathParameters ?? []).Where(parameter =>
            operationParameters?.Any(overridden => overridden.Name == parameter.Name && overridden.In == parameter.In) != true));
    }
}
