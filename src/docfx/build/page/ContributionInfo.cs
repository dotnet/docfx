// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class ContributionInfo
    {
        public string? UpdateAt { get; set; }

        public DateTime UpdatedAtDateTime { get; set; }

        public Contributor[]? Contributors { get; set; }

        public Contributor? Author { get; set; }
    }
}
