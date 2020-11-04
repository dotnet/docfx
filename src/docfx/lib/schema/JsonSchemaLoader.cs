// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

            return LoadSchema(json, new FilePath(path));
        }

        public JsonSchema? LoadSchema(Package package, PathString path)
        {
            return LoadSchema(package.ReadString(path), new FilePath(path));
        }

        public JsonSchema LoadSchema(SourceInfo<string> url)
        {
            return LoadSchema(_fileResolver.ReadString(url), new FilePath(url));
        }

        public JsonSchema LoadSchema(string json, FilePath file)
        {
            var token = JToken.Parse(json);
            var schemaMap = new Dictionary<JToken, JsonSchema>(ReferenceEqualsComparer.Default);
            var schema = Deserialize(token, schemaMap);

            var jsonPath = new Dictionary<string, JsonSchema>();
            LoadJsonPath("#", token, schemaMap, jsonPath);

            schema.ReferenceResolver = new JsonSchemaReferenceResolver(jsonPath);
            return schema;
        }

        private void LoadJsonPath(string jsonPath, JToken token, Dictionary<JToken, JsonSchema> schemaMap, Dictionary<string, JsonSchema> result)
        {
            if (schemaMap.TryGetValue(token, out var schema))
            {
                result.TryAdd(jsonPath, schema);
            }

            switch (token)
            {
                case JArray array:
                    for (var i = 0; i < array.Count; i++)
                    {
                        LoadJsonPath(string.Concat(jsonPath, "/", i), array[i], schemaMap, result);
                    }
                    break;

                case JObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (value != null)
                        {
                            LoadJsonPath(string.Concat(jsonPath, "/", key), value, schemaMap, result);
                        }
                    }
                    break;
            }
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
    }
}
