// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            if (string.IsNullOrEmpty(json))
            {
                return new JsonSchema();
            }

            var token = JToken.Parse(json);
            var schema = JsonUtility.ToObject<JsonSchema>(ErrorBuilder.Null, token);
            schema.ReferenceResolver = new JsonSchemaReferenceResolver(token, schema.Id);
            return schema;
        }
    }
}
