// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class PublishModel
{
    public string? Name { get; init; }

    public string? Product { get; init; }

    public string? BasePath { get; init; }

    public string? ThemeBranch { get; init; }

    public PublishItem[] Files { get; init; } = Array.Empty<PublishItem>();

    public IReadOnlyDictionary<string, MonikerList>? MonikerGroups { get; init; }
}
