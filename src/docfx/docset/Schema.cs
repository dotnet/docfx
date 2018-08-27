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
        public Type Type { get; }

        public DataSchemaAttribute Attribute { get; }

        public string Name => Type.Name;

        private Schema(Type type)
        {
            Type = type;
            Attribute = type.GetCustomAttribute<DataSchemaAttribute>();
        }

        private static readonly IReadOnlyDictionary<string, Schema> s_schemas =
            typeof(PageModel).Assembly.ExportedTypes
            .Where(type => type.GetCustomAttribute<DataSchemaAttribute>() != null)
            .ToDictionary(item => item.Name, item => new Schema(item), StringComparer.OrdinalIgnoreCase);

        public static Schema GetSchema(string mime)
        {
            return mime != null && s_schemas.TryGetValue(mime, out var result) ? result : null;
        }

        public static (string mime, Schema schema) ReadFromFile(string filePath)
        {
            string mime = null;

            if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        mime = JsonUtility.ReadMime(reader);
                    }
                }
            }
            else if (filePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        mime = YamlUtility.ReadMime(reader);
                    }
                }
            }

            if (mime != null && s_schemas.TryGetValue(mime, out var schema))
            {
                return (mime, schema);
            }

            return default;
        }
    }
}
