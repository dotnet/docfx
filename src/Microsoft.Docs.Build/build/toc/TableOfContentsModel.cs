// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsModel
    {
        public TableOfContentsMetadata Metadata { get; }

        public TableOfContentsNode[] Items { get; }

        [JsonProperty("_path")]
        public string Path { get; }

        public TableOfContentsModel(TableOfContentsNode[] items, TableOfContentsMetadata metadata, string path)
        {
            Items = items;
            Metadata = metadata;
            Path = path;
        }
    }
}
