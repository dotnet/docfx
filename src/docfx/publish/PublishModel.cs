// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class PublishModel
    {
        public PublishItem[] Files { get; set; }

        public Dictionary<string, List<string>> MonikerGroups { get; set; } = new Dictionary<string, List<string>>();
    }
}
