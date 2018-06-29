// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

internal class CommitsHistoryItem
{
    [JsonProperty("sha")]
    public string Sha { get; set; }

    [JsonProperty("built_at")]
    public DateTime BuiltAt { get; set; }
}
