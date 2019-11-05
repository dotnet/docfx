// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class RedirectionItem
    {
        public string SourcePath { get; set; }

        public SourceInfo<string> RedirectUrl { get; set; }

        public bool RedirectDocumentId { get; set; }
    }
}
