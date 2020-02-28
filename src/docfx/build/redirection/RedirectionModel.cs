// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class RedirectionModel
    {
        [JsonConverter(typeof(UnionTypeConverter))]
        public (Dictionary<PathString, SourceInfo<string>>? objectForm, RedirectionItem[]? arrayForm) Redirections { get; set; }

        public Dictionary<PathString, SourceInfo<string>> Renames { get; } = new Dictionary<PathString, SourceInfo<string>>();
    }
}
