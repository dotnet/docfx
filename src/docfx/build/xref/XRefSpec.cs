// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class XrefSpec
    {
        public string Uid { get; set; }

        public string Name { get; set; }

        public string Href { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; }
    }
}
