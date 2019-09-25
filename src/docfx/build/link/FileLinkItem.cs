// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class FileLinkItem
    {
        public string SourceUrl { get; set; }

        public string SourceMonikerGroup { get; set; }

        public string TargetUrl { get; set; }
    }
}
