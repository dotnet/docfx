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
        public string Id { get; private set; } = "";

        public string Title { get; private set; } = "";

        public string Prompt { get; private set; } = "";

        public List<ZonePivot> Pivots { get; private set; } = new List<ZonePivot>();
    }
}
