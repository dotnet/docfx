// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class ZonePivotGroup
    {
        public string Id { get; } = "";

        public string Title { get; } = "";

        public string Prompt { get; } = "";

        public List<ZonePivot> Pivots { get; } = new List<ZonePivot>();
    }
}
