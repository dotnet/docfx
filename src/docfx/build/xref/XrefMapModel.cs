// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class XrefMapModel
{
    public ExternalXrefSpec[] References { get; init; } = Array.Empty<ExternalXrefSpec>();

    public ExternalXref[] ExternalXrefs { get; init; } = Array.Empty<ExternalXref>();

    public XrefProperties? Properties { get; init; }

    public string? RepositoryUrl { get; init; }

    public string? DocsetName { get; init; }

    public IReadOnlyDictionary<string, MonikerList>? MonikerGroups { get; init; }
}
