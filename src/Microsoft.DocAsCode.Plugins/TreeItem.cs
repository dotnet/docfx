// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class TreeItem
{
    [JsonProperty("items")]
    public List<TreeItem> Items { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
