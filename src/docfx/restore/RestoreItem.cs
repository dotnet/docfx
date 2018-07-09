// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    /// todo: add other lock items like githuber users, build updated_at
    /// <summary>
    /// It's like your package-lock.json which lock every version of your referencing item like
    /// 1. dependeny repo version
    /// 2. github user cache version
    /// 3. build publish time cahe version
    /// </summary>
    internal class RestoreItem
    {
        [JsonProperty(PropertyName = "git")]
        public Dictionary<string, string> Git { get; set; } = new Dictionary<string, string>();
    }
}
