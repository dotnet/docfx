// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class MicrosoftGraphUser : ICacheObject<string>
    {
        public string? Alias { get; set; }

        public string? Id { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public IEnumerable<string> GetKeys()
        {
            if (Alias != null)
            {
                yield return Alias;
            }
        }
    }
}
