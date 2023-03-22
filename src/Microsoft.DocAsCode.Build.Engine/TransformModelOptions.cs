// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Build.Engine;

public class TransformModelOptions
{
    [JsonProperty(PropertyName = "isShared")]
    public bool IsShared { get; set; }

    [JsonProperty(PropertyName = "bookmarks")]
    public Dictionary<string, string> Bookmarks { get; set; }
}
