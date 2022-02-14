// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class JsonSchemaResolver
{
    private static readonly ThreadLocal<JsonSchemaResolver?> s_current = new();

    private readonly JToken _schema;
    private readonly Uri _baseUrl = new("https://me");
    private readonly Dictionary<string, JToken> _schemasById = new();
    private readonly Func<Uri, Uri, JsonSchema?> _resolveExternalSchema;

    private readonly ConcurrentDictionary<string, JsonSchema?> _references = new();

    public static readonly JsonSchemaResolver Null = new(new JObject(), (a, b) => null);

    internal static JsonSchemaResolver Current => s_current.Value ?? throw new InvalidOperationException("Use JsonSchemaLoader to load JSON schema");

    internal JsonSchemaResolver(JToken schema, Func<Uri, Uri, JsonSchema?> resolveExternalSchema)
    {
        _schema = schema;
        _resolveExternalSchema = resolveExternalSchema;

        if (schema is JObject obj && obj.TryGetValue<JValue>("$id", out var id) && id.Value is string schemaId)
        {
            _baseUrl = new(_baseUrl, schemaId);
        }

        ExpandSchemaIdAndRef(_baseUrl, _schema, _schemasById);
    }

    public JsonSchema? ResolveSchema(JsonSchema? schema)
    {
        return string.IsNullOrEmpty(schema?.Ref) ? schema : ResolveSchema(schema.Ref) ?? schema;
    }

    public JsonSchema? ResolveSchema(string schemaRef)
    {
        return _references.GetOrAdd(schemaRef, schemaRef => ResolveSchemaCore(schemaRef, new HashSet<string>()));
    }

    private JsonSchema? ResolveSchemaCore(string schemaRef, HashSet<string> recursions)
    {
        if (!recursions.Add(schemaRef))
        {
            return JsonSchema.FalseSchema;
        }

        // Lookup by id
        if (_schemasById.TryGetValue(schemaRef, out var token))
        {
            return DeserializeSchema(recursions, token);
        }

        // Lookup by JSON pointer if it is within the same document
        var schemaRefUrl = new Uri(_baseUrl, schemaRef);
        if (schemaRefUrl == _baseUrl)
        {
            return LookupJsonPointer(schemaRefUrl.Fragment, out token) ? DeserializeSchema(recursions, token) : null;
        }

        // Lookup other documents
        var externalSchema = _resolveExternalSchema(_baseUrl, schemaRefUrl);

        return externalSchema?.SchemaResolver?.ResolveSchema(schemaRefUrl.Fragment);
    }

    private JsonSchema DeserializeSchema(HashSet<string> recursions, JToken token)
    {
        try
        {
            s_current.Value = this;

            var resolvedSchema = JsonUtility.ToObject<JsonSchema>(ErrorBuilder.Null, token);
            return string.IsNullOrEmpty(resolvedSchema.Ref)
                ? resolvedSchema
                : ResolveSchemaCore(resolvedSchema.Ref, recursions) ?? resolvedSchema;
        }
        finally
        {
            s_current.Value = null;
        }
    }

    private bool LookupJsonPointer(string fragment, [NotNullWhen(true)] out JToken? token)
    {
        token = _schema;

        var jsonPointer = HttpUtility.UrlDecode(fragment.TrimStart('#'));

        foreach (var key in jsonPointer.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            token = token switch
            {
                // https://tools.ietf.org/html/rfc6901
                // This is performed by first transforming any
                // occurrence of the sequence '~1' to '/', and then transforming any
                // occurrence of the sequence '~0' to '~'.
                JObject obj => obj[key.Replace("~1", "/").Replace("~0", "~")],
                JArray array when int.TryParse(key, out var i) && i >= 0 && i < array.Count => array[i],
                _ => null,
            };

            if (token is null)
            {
                break;
            }
        }

        return token != null;
    }

    private void ExpandSchemaIdAndRef(Uri baseUrl, JToken token, Dictionary<string, JToken> schemasById)
    {
        switch (token)
        {
            case JArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    ExpandSchemaIdAndRef(baseUrl, array[i], schemasById);
                }
                break;

            case JObject obj:
                if (obj.TryGetValue<JValue>("$id", out var id) && id.Value is string schemaId)
                {
                    baseUrl = new Uri(baseUrl, schemaId);
                    schemasById.TryAdd(baseUrl.ToString().TrimEnd('/', '#'), obj);
                }

                if (obj.TryGetValue<JValue>("$ref", out var @ref) && @ref.Value is string schemaRef)
                {
                    obj["$ref"] = new Uri(baseUrl, schemaRef).ToString().TrimEnd('/', '#');
                }

                foreach (var (_, value) in obj)
                {
                    if (value != null)
                    {
                        ExpandSchemaIdAndRef(baseUrl, value, schemasById);
                    }
                }
                break;
        }
    }
}
