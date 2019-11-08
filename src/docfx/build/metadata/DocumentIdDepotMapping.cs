// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class DocumentIdDepotMapping
    {
        public string FolderRelativePathInDocset { get; set; }

        public string DepotName { get; set; }
    }
}
