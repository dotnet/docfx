// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal class FileMappingConfig
{
    /// <summary>
    /// Gets the file glob patterns included by the group.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public string[] Files { get; init; } = Config.DefaultInclude;

    /// <summary>
    /// Gets the file glob patterns excluded from the group.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public string[] Exclude { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the root folder.
    /// </summary>
    public PathString Src { get; init; }

    /// <summary>
    /// Gets the destination folder if copy/transform is used.
    /// </summary>
    public PathString Dest { get; init; }

    /// <summary>
    /// Gets the group name for v2 backward compact
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Gets the version name for v2 backward compact
    /// </summary>
    public SourceInfo<string?> Version { get; init; }
}
