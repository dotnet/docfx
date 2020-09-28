// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    internal class SearchIndexItem
    {
        public string Id { get; }

        public string? Title { get; set; }

        public string? Body { get; set; }

        public SearchIndexItem(string id) => Id = id;
    }
}
