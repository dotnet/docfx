// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaReferenceResolver
    {
        private readonly JToken _schema;
        private readonly Uri _baseUrl;
        private readonly Dictionary<string, JToken> _schemasById = new Dictionary<string, JToken>();

        private readonly ConcurrentDictionary<string, JsonSchema?> _references = new ConcurrentDictionary<string, JsonSchema?>();

        public static readonly JsonSchemaReferenceResolver Null = new JsonSchemaReferenceResolver(new JObject());

        public JsonSchemaReferenceResolver(JToken schema, string? id = null)
        {
            _schema = schema;
            _baseUrl = new Uri(new Uri("https://me"), id);

            LoadSchemasById(_baseUrl, schema, _schemasById);
        }

        public JsonSchema? ResolveSchema(string schemaRef)
        {
            return _references.GetOrAdd(schemaRef, schemaRef => ResolveSchemaCore(schemaRef, new HashSet<string>()));
        }

        private JsonSchema? ResolveSchemaCore(string schemaRef, HashSet<string> recursions)
        {
            var thisUrl = new Uri(_baseUrl, schemaRef);
            var url = thisUrl.ToString().TrimEnd('/', '#');
            if (!recursions.Add(url))
            {
                return JsonSchema.FalseSchema;
            }

            // Lookup by id then by JSON pointer
            if (_schemasById.TryGetValue(url, out var token) || LookupJsonPointer(thisUrl.Fragment, out token))
            {
                var resolvedSchema = JsonUtility.ToObject<JsonSchema>(ErrorBuilder.Null, token);
                return string.IsNullOrEmpty(resolvedSchema.Ref)
                    ? resolvedSchema
                    : ResolveSchemaCore(resolvedSchema.Ref, recursions) ?? resolvedSchema;
            }

            return null;
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

        private void LoadSchemasById(Uri baseUrl, JToken token, Dictionary<string, JToken> schemasById)
        {
            switch (token)
            {
                case JArray array:
                    for (var i = 0; i < array.Count; i++)
                    {
                        LoadSchemasById(baseUrl, array[i], schemasById);
                    }
                    break;

                case JObject obj:
                    if (obj.TryGetValue<JValue>("$id", out var id) && id.Value is string schemaId)
                    {
                        baseUrl = new Uri(baseUrl, schemaId);
                        schemasById.TryAdd(baseUrl.ToString().TrimEnd('/', '#'), obj);
                    }

                    foreach (var (_, value) in obj)
                    {
                        if (value != null)
                        {
                            LoadSchemasById(baseUrl, value, schemasById);
                        }
                    }
                    break;
            }
        }
    }
}
