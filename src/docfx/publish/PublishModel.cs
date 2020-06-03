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
        public string Name { get; }

        public string Product { get; }

        public string BasePath { get; }

        public PublishItem[] Files { get; }

        public IReadOnlyDictionary<string, MonikerList> MonikerGroups { get; }

        public PublishModel(
            string name,
            string product,
            string basePath,
            PublishItem[] files,
            IReadOnlyDictionary<string, MonikerList> monikerGroups)
        {
            Name = name;
            Product = product;
            BasePath = basePath;
            Files = files;
            MonikerGroups = monikerGroups;
        }
    }
}
