// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal class TocModel
{
    public TocMetadata Metadata { get; }

    public TocNode[] Items { get; }

    [JsonProperty("_path")]
    public string Path { get; }

    public string Schema { get; } = "toc";

    public TocModel(TocNode[] items, TocMetadata metadata, string path)
    {
        Items = items;
        Metadata = metadata;
        Path = path;
    }
}
