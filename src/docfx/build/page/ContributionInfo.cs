// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class ContributionInfo
    {
        public string UpdateAt { get; set; }

        public DateTime UpdatedAtDateTime { get; set; }

        public List<Contributor> Contributors { get; set; }

        public Contributor Author { get; set; }
    }
}
