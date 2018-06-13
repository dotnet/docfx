// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class PageMetadata
    {
        public string Title { get; set; }

        public string RedirectionUrl { get; set; }

        [JsonExtensionData]
        public JObject Metadata { get; set; }
    }
}
