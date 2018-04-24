// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;

    using Newtonsoft.Json;

    public class UidDefinition
    {
        [JsonProperty("name")]
        public string Name { get; }
        [JsonProperty("file")]
        public string File { get; }
        [JsonProperty("line")]
        public int? Line { get; }
        [JsonProperty("column")]
        public int? Column { get; }
        [JsonProperty("path")]
        public string Path { get; }

        [JsonConstructor]
        public UidDefinition(string name, string file, int? line = null, int? column = null, string path = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Uid name cannot be null or empty.", nameof(name));
            }

            Name = name;
            File = file;
            Line = line;
            Column = column;
            Path = path;
        }

        [Obsolete]
        public UidDefinition(string name, string file, int? line, int? column)
        {
            Name = name;
            File = file;
            Line = line;
            Column = column;
        }
    }
}
