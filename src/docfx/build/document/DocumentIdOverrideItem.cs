// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class DocumentIdOverrideItem
{
    public string DepotName { get; init; } = "";

    public string SourcePath { get; init; } = "";

    public string DocumentId { get; init; } = "";

    public string DocumentVersionIndependentId { get; init; } = "";
}
