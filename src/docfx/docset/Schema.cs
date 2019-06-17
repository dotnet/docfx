// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class Schema
    {
        public string Name { get; }

        // todo: read schema from template
        private static readonly Dictionary<string, Schema> s_schemas = Directory.EnumerateFiles("data", "*.json", SearchOption.TopDirectoryOnly).ToDictionary(k => Path.GetFileNameWithoutExtension(k), v => new Schema(Path.GetFileNameWithoutExtension(v)));

        private Schema(string name)
        {
            Name = name;
        }

        public bool Is(Type type) => string.Equals(type.Name, Name, StringComparison.OrdinalIgnoreCase);

        public static SourceInfo<string> ReadFromFile(string pathToDocset, string filePath)
        {
            SourceInfo<string> mime = null;

            if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        mime = JsonUtility.ReadMime(reader, pathToDocset);
                    }
                }
            }
            else if (filePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        mime = new SourceInfo<string>(YamlUtility.ReadMime(reader), new SourceInfo(pathToDocset, 1, 1));
                    }
                }
            }

            return mime;
        }

        public static string GetSchemaName(string mime)
        {
            if (mime != null && s_schemas.TryGetValue(mime, out var schema))
            {
                return schema.Name;
            }

            return default;
        }

        public static bool IsPage(string mime)
        {
            if (mime != null && s_schemas.TryGetValue(mime, out var schema))
            {
                return !string.Equals(schema.Name, "ContextObject", StringComparison.OrdinalIgnoreCase) && !string.Equals(schema.Name, "TestData", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public static bool Is(string mime, Type type)
        {
            if (mime != null && s_schemas.TryGetValue(mime, out var schema))
            {
                return schema.Is(type);
            }

            return false;
        }
    }
}
