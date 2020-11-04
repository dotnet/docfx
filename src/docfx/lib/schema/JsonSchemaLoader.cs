// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaLoader
    {
        private readonly FileResolver _fileResolver;

        public JsonSchemaLoader(FileResolver fileResolver)
        {
            _fileResolver = fileResolver;
        }

        public JsonSchema? TryLoadSchema(Package package, PathString path)
        {
            var json = package.TryReadString(path);
            if (json is null)
            {
                return null;
            }

            return LoadSchema(json);
        }

        public JsonSchema? LoadSchema(Package package, PathString path)
        {
            return LoadSchema(package.ReadString(path));
        }

        public JsonSchema LoadSchema(SourceInfo<string> url)
        {
            return LoadSchema(_fileResolver.ReadString(url));
        }

        public JsonSchema LoadSchema(string json)
        {
            var token = JToken.Parse(json);
            var schemaMap = new Dictionary<JToken, JsonSchema>(ReferenceEqualsComparer.Default);
            var schema = Deserialize(token, schemaMap);

            var baseUrl = new Uri(new Uri("https://me"), schema.Id);
            var definitions = new Dictionary<string, JsonSchema>();
            LoadSchemasByJsonPath(new UriBuilder(baseUrl), "#", token, schemaMap, definitions);
            LoadSchemasById(baseUrl, token, schemaMap, definitions);

            schema.ReferenceResolver = new JsonSchemaReferenceResolver(baseUrl, definitions);
            return schema;
        }

        private static JsonSchema Deserialize(JToken token, Dictionary<JToken, JsonSchema> schemaMap)
        {
            try
            {
                JsonSchemaConverter.OnJsonSchema = schemaMap.Add;
                return JsonUtility.ToObject<JsonSchema>(ErrorBuilder.Null, token);
            }
            finally
            {
                JsonSchemaConverter.OnJsonSchema = null;
            }
        }

        private void LoadSchemasByJsonPath(
            UriBuilder baseUrl, string jsonPath, JToken token, Dictionary<JToken, JsonSchema> schemaMap, Dictionary<string, JsonSchema> definitions)
        {
            if (schemaMap.TryGetValue(token, out var schema))
            {
                baseUrl.Fragment = jsonPath;
                definitions.TryAdd(baseUrl.Uri.ToString().TrimEnd('/', '#'), schema);
            }

            switch (token)
            {
                case JArray array:
                    for (var i = 0; i < array.Count; i++)
                    {
                        LoadSchemasByJsonPath(baseUrl, string.Concat(jsonPath, "/", i), array[i], schemaMap, definitions);
                    }
                    break;

                case JObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (value != null)
                        {
                            LoadSchemasByJsonPath(baseUrl, string.Concat(jsonPath, "/", key), value, schemaMap, definitions);
                        }
                    }
                    break;
            }
        }

        private void LoadSchemasById(
            Uri baseUrl, JToken token, Dictionary<JToken, JsonSchema> schemaMap, Dictionary<string, JsonSchema> definitions)
        {
            if (schemaMap.TryGetValue(token, out var schema))
            {
                baseUrl = new Uri(baseUrl, schema.Id);
                definitions.TryAdd(baseUrl.ToString().TrimEnd('/', '#'), schema);
            }

            switch (token)
            {
                case JArray array:
                    for (var i = 0; i < array.Count; i++)
                    {
                        LoadSchemasById(baseUrl, array[i], schemaMap, definitions);
                    }
                    break;

                case JObject obj:
                    foreach (var (_, value) in obj)
                    {
                        if (value != null)
                        {
                            LoadSchemasById(baseUrl, value, schemaMap, definitions);
                        }
                    }
                    break;
            }
        }
    }
}
