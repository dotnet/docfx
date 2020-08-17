// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class OpsJoinTocConfig
    {
        public string? ReferenceTOC { get; private set; }

        public string? ReferenceTOCUrl { get; private set; }

        public string? TopLevelTOC { get; private set; }

        public string? ConceptualTOCUrl { get; private set; }

        public string? ConceptualTOC { get; private set; }

        public string? OutputFolder { get; private set; }

        public JObject? ContainerPageMetadata { get; private set; }
    }
}
