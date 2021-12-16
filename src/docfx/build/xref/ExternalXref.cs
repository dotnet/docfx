// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Docs.Build;

internal class ExternalXref
{
    public string Uid { get; set; } = "";

    public string? DocsetName { get; set; }

    public int Count { get; set; }

    public string? SchemaType { get; set; }

    public string? PropertyPath { get; set; }

    [JsonIgnore]
    public string? ReferencedRepositoryUrl { get; set; }
}
