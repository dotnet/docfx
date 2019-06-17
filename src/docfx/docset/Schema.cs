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
        public static readonly Schema Conceptual = new Schema(typeof(Conceptual));

        // todo: get page type from json schema
        public bool IsPage => !string.Equals(Name, "ContextObject", StringComparison.OrdinalIgnoreCase) && !string.Equals(Name, "TestData", StringComparison.OrdinalIgnoreCase);

        public string Name { get; }

        // todo: read schema from template
        private static readonly Dictionary<string, Schema> s_schemas = Directory.EnumerateFiles("data", "*.json", SearchOption.TopDirectoryOnly).ToDictionary(k => Path.GetFileNameWithoutExtension(k), v => new Schema(Path.GetFileNameWithoutExtension(v)));

        private Schema(string name)
        {
            Name = name;
        }

        private Schema(Type type)
            : this(type.Name)
        {
        }

        public bool Is(Type type) => string.Equals(type.Name, Name, StringComparison.OrdinalIgnoreCase);

        public static (SourceInfo<string> mime, Schema schema) ReadFromFile(string pathToDocset, string filePath)
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

            if (mime?.Value != null && s_schemas.TryGetValue(mime, out var schema))
            {
                return (mime, schema);
            }

            return (mime, null);
        }
    }
}
