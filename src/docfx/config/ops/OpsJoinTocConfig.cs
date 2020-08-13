// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class OpsJoinTocConfig
    {
        public string? ReferenceTOC { get; set; }

        public string? ReferenceTOCUrl { get; set; }

        public string? TopLevelTOC { get; set; }

        public string? ConceptualTOCUrl { get; set; }

        public string? ConceptualTOC { get; set; }

        public string? OutputFolder { get; set; }

        public JObject? ContainerPageMetadata { get; set; }
    }
}
