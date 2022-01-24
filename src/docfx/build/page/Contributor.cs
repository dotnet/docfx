// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class Contributor : IEquatable<Contributor>
{
    public string? Name { get; set; }

    public string? ProfileUrl { get; set; }

    public string? DisplayName { get; set; }

    public string? Id { get; set; }

    public bool Equals(Contributor? other) => Id == other?.Id;

    public override bool Equals(object? obj) => Equals(obj as Contributor);

    public override int GetHashCode() => Id is null ? 0 : Id.GetHashCode();
}
