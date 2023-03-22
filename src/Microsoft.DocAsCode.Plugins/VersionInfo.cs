// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class VersionInfo
{
    [JsonProperty("version_folder")]
    public string VersionFolder { get; set; }
}
