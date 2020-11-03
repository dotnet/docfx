// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaLoader
    {
        private readonly FileResolver _fileResolver;

        public JsonSchemaLoader(FileResolver fileResolver)
        {
            _fileResolver = fileResolver;
        }

        public JsonSchema LoadSchema(SourceInfo<string> url)
        {
            return LoadSchemaCore(_fileResolver.ReadString(url), new FilePath(url));
        }

        public JsonSchema? LoadSchema(Package package, PathString path)
        {
            return LoadSchemaCore(package.ReadString(path), new FilePath(path));
        }

        public JsonSchema? TryLoadSchema(Package package, PathString path)
        {
            var json = package.TryReadString(path);
            if (json is null)
            {
                return null;
            }

            return LoadSchemaCore(json, new FilePath(path));
        }

        private JsonSchema LoadSchemaCore(string json, FilePath file)
        {
            var token = JsonUtility.Parse(ErrorBuilder.Null, json, file);
            var schema = JsonUtility.ToObject<JsonSchema>(ErrorBuilder.Null, token);

            return ResolveRef(schema);
        }

        private JsonSchema ResolveRef(JsonSchema schema)
        {
            return schema;
        }
    }
}
