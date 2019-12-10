// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class PublishModel
    {
        public string Name { get; set; }

        public string Product { get; set; }

        public string BaseUrl { get; set; }

        public PublishItem[] Files { get; set; }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> MonikerGroups { get; set; } = new Dictionary<string, IReadOnlyList<string>>();
    }
}
