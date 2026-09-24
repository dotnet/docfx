// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using static Docfx.Build.RestApi.RestApiModelUtility;

namespace Docfx.Build.RestApi;

internal sealed class OpenApi3ModelConverter(Uri documentUri, IReadOnlyDictionary<string, string> constants)
{
    internal RestApiRootItemViewModel Convert(OpenApiDocument document, string raw, string version)
    {
        var servers = Servers(document.Servers);
        var server = servers[0].Url;
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
        model.SpecificationVersion = version;
        model.Servers = servers;
        model.Info = Serialize(document.Info).ToObject<RestApiInfoViewModel>();
        if (document.ExternalDocs != null)
        {
            model.ExternalDocs = Serialize(document.ExternalDocs).ToObject<RestApiExternalDocumentationViewModel>();
        }
        var schemas = new Dictionary<string, RestApiSchemaViewModel>();
        foreach (var (name, schema) in document.Components?.Schemas?.AsEnumerable() ?? [])
        {
            schemas[name] = Schema(schema);
        }
        model.Schemas = schemas;
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
                var parameters = MergeParameters(operation.Parameters, pathItem.Parameters,
                    (left, right) => left.Name == right.Name && left.In == right.In);
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
                child.Servers = effectiveServers;
                child.RequestUrl = effectiveServers[0].Url.TrimEnd('/') + "/" + path.TrimStart('/');
                if (operation.RequestBody is { } body)
                {
                    child.RequestBody = new RestApiRequestBodyViewModel
                    {
                        Description = body.Description,
                        Required = body.Required,
                        Content = Content(body.Content)
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

    private static List<RestApiServerViewModel> Servers(IList<OpenApiServer> servers)
    {
        if (servers == null || servers.Count == 0)
        {
            return [new() { Url = "/" }];
        }
        return servers.Select(server =>
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
            return new RestApiServerViewModel { Url = url, Description = server.Description };
        }).ToList();
    }

    private RestApiParameterViewModel Parameter(IOpenApiParameter parameter)
    {
        var schema = Schema(parameter.Schema);
        var metadata = Extensions(parameter.Extensions);
        metadata["in"] = parameter.In?.ToString().ToLowerInvariant();
        metadata["required"] = parameter.Required;
        metadata["style"] = parameter.Style?.ToString();
        metadata["explode"] = parameter.Explode;
        if (parameter.Schema?.Default != null)
        {
            metadata["default"] = Literal(parameter.Schema.Default);
        }
        return new RestApiParameterViewModel
        {
            Name = parameter.Name,
            Description = parameter.Description,
            Schema = schema,
            Content = parameter.Content is { Count: > 0 } ? Content(parameter.Content) : null,
            Metadata = metadata
        };
    }

    private RestApiResponseViewModel Response(string status, IOpenApiResponse response)
    {
        return new RestApiResponseViewModel
        {
            HttpStatusCode = status,
            Description = response.Description,
            Metadata = Extensions(response.Extensions),
            Content = Content(response.Content)
        };
    }

    private List<RestApiMediaTypeViewModel> Content(IDictionary<string, IOpenApiMediaType> content) =>
        content?.Select(pair => new RestApiMediaTypeViewModel
        {
            MimeType = pair.Key,
            Schema = Schema(pair.Value.Schema),
            ItemSchema = Schema(pair.Value.ItemSchema),
            Examples = Examples(pair.Key, pair.Value)
        }).ToList() ?? [];

    private static List<RestApiResponseExampleViewModel> Examples(string mimeType, IOpenApiMediaType media)
    {
        var result = new List<RestApiResponseExampleViewModel>();
        if (media.Example != null)
        {
            result.Add(new() { MimeType = mimeType, Content = Literal(media.Example) });
        }
        foreach (var (name, example) in media.Examples?.AsEnumerable() ?? [])
        {
            result.Add(new()
            {
                Name = name,
                MimeType = mimeType,
                Content = example.SerializedValue ?? Literal(example.DataValue ?? example.Value),
                ExternalValue = example.ExternalValue
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

    private RestApiSchemaViewModel Schema(IOpenApiSchema schema, HashSet<IOpenApiSchema> ancestors = null)
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
                return new() { Type = "recursive reference", LoopReferenceName = ReferenceName(reference) };
            }
            try
            {
                var targetModel = Schema(target, ancestors);
                var siblings = GetReferenceSiblings(reference);
                using var siblingText = new StringWriter();
                siblings.SerializeAsV32(new OpenApiJsonWriter(siblingText));
                var result = JObject.Parse(siblingText.ToString()).Count == 0 ? targetModel : new RestApiSchemaViewModel
                {
                    Type = "all of",
                    Description = reference.Reference.Description ?? target.Description,
                    AllOf = [targetModel, Schema(siblings, ancestors)]
                };
                result.ReferenceName ??= ReferenceName(reference);
                return result;
            }
            finally
            {
                ancestors.Remove(reference);
            }
        }
        if (!ancestors.Add(schema))
        {
            return new() { Type = "recursive reference", LoopReferenceName = schema.Title ?? "schema" };
        }

        try
        {
            using var text = new StringWriter();
            schema.SerializeAsV32(new OpenApiJsonWriter(text));
            var serialized = JToken.Parse(text.ToString());
            RestoreConstants(serialized);
            if (serialized is JObject { Count: 1 } && serialized["not"] is JObject { Count: 0 })
            {
                return new() { Type = "no value" };
            }

            var result = new RestApiSchemaViewModel
            {
                Metadata = Extensions(schema.Extensions),
                Type = schema.Type?.ToString().ToLowerInvariant().Replace(", ", " | ") ??
                    (serialized is JObject { Count: 0 } ? "any value" : "any type"),
                Format = schema.Format,
                Description = schema.Description,
                Properties = schema.Properties?.ToDictionary(pair => pair.Key, pair =>
                {
                    var property = Schema(pair.Value, ancestors);
                    if (schema.Required?.Contains(pair.Key) == true)
                    {
                        property.Required = true;
                    }
                    return property;
                }),
                Items = Schema(schema.Items, ancestors)
            };
            var composition = new List<RestApiSchemaCompositionViewModel>();
            result.AllOf = schema.AllOf is { Count: > 0 } ? schema.AllOf.Select(s => Schema(s, ancestors)).ToList() : null;
            AddComposition("One of", schema.OneOf);
            AddComposition("Any of", schema.AnyOf);
            if (schema.Not != null)
            {
                AddComposition("Not", [schema.Not]);
            }
            if (composition.Count > 0)
            {
                result.Composition = composition;
            }
            var constraints = new List<RestApiSchemaConstraintViewModel>();
            foreach (var property in ((JObject)serialized).Properties())
            {
                if (!property.Name.StartsWith("x-", StringComparison.Ordinal) && property.Name is not
                    ("type" or "format" or "description" or "properties" or "items" or "allOf" or "oneOf" or "anyOf" or "not" or
                    "additionalProperties" or "enum" or "example" or "examples"))
                {
                    constraints.Add(new() { Name = property.Name, Value = property.Value.ToString(Formatting.None) });
                }
            }
            if (schema.AdditionalProperties != null)
            {
                composition.Add(new() { Kind = "Additional properties", Schemas = [Schema(schema.AdditionalProperties, ancestors)] });
                result.Composition = composition;
            }
            else if (!schema.AdditionalPropertiesAllowed)
            {
                constraints.Add(new() { Name = "additionalProperties", Value = "false" });
            }
            if (constraints.Count > 0)
            {
                result.Constraints = constraints;
            }
            if (schema.Enum is { Count: > 0 })
            {
                result.Enum = schema.Enum.Select(value => value == null ? null : (object)JToken.Parse(Literal(value))).ToList();
            }
            if (schema.Examples is { Count: > 0 })
            {
                result.Examples = schema.Examples.Select(example => new RestApiResponseExampleViewModel { Content = Literal(example) }).ToList();
            }
#pragma warning disable CS0618 // OpenAPI 3.0's singular schema example is still read into this SDK property.
            else if (schema.Example != null)
            {
                result.Examples = [new() { Content = Literal(schema.Example) }];
            }
#pragma warning restore CS0618
            return result;

            void AddComposition(string kind, IList<IOpenApiSchema> schemas)
            {
                if (schemas is { Count: > 0 })
                {
                    composition.Add(new() { Kind = kind, Schemas = schemas.Select(s => Schema(s, ancestors)).ToList() });
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

    private string ReferenceName(OpenApiSchemaReference reference)
    {
        var host = reference.Reference.HostDocument?.BaseUri ?? documentUri;
        var target = reference.Reference.ExternalResource is { } external ? new Uri(host, external) : host;
        return target == documentUri ? reference.Reference.Id :
            documentUri.MakeRelativeUri(target) + "#" + reference.Reference.Id;
    }
}
