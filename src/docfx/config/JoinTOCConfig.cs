// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JoinTOCConfig
    {
        public string? OutputPath { get; set; }

        public JObject? ContainerPageMetadata { get; set; }

        public string? ReferenceToc { get; set; }

        public string? TopLevelToc { get; set; }
    }
}
