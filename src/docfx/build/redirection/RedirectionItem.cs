// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class RedirectionItem
{
    public PathString SourcePath { get; set; }

    public PathString SourcePathFromRoot { get; set; }

    [JsonConverter(typeof(OneOrManyConverter))]
    public SourceInfo<string>[]? Monikers { get; set; }

    public SourceInfo<string> RedirectUrl { get; set; } = new SourceInfo<string>("");

    public bool RedirectDocumentId { get; set; }
}
