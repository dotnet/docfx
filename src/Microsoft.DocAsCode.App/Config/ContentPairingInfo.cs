// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode;

[Serializable]
internal class ContentPairingInfo
{
    [JsonProperty("contentFolder")]
    public string ContentFolder { get; set; }

    [JsonProperty("overwriteFragmentsFolder")]
    public string OverwriteFragmentsFolder { get; set; }
}