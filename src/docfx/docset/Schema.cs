// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Docs.Build
{
    internal class Schema
    {
        public static readonly Schema Conceptual = new Schema(typeof(Conceptual));

        public Type Type { get; }

        public DataSchemaAttribute Attribute { get; }

        public string Name => Type.Name;

        private Schema(Type type)
        {
            Type = type;
            Attribute = type.GetCustomAttribute<DataSchemaAttribute>();
        }

        private static readonly IReadOnlyDictionary<string, Schema> s_schemas =
            typeof(TestData).Assembly.ExportedTypes
            .Where(type => type.GetCustomAttribute<DataSchemaAttribute>() != null)
            .ToDictionary(item => item.Name, item => new Schema(item), StringComparer.OrdinalIgnoreCase);

        public static Schema GetSchema(string mime)
        {
            return mime != null && s_schemas.TryGetValue(mime, out var result) ? result : null;
        }

        public static (SourceInfo<string> mime, Schema schema) ReadFromFile(string pathToDocset, string filePath)
        {
            SourceInfo<string> mime = null;

            if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        var mimeContent = JsonUtility.ReadMime(reader);
                        mime = new SourceInfo<string>(mimeContent, new SourceInfo(pathToDocset, mimeContent is null ? 1 : 2, 0));
                    }
                }
            }
            else if (filePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        mime = new SourceInfo<string>(YamlUtility.ReadMime(reader), new SourceInfo(pathToDocset, 1, 0));
                    }
                }
            }

            if (mime?.Value != null && s_schemas.TryGetValue(mime, out var schema))
            {
                return (mime, schema);
            }

            return (mime, null);
        }
    }
}
