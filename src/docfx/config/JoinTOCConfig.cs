// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JoinTOCConfig
    {
        public PathString OutputFolder { get; private set; }

        public JObject? ContainerPageMetadata { get; private set; }

        public string? ReferenceToc { get; private set; }

        public string? TopLevelToc { get; private set; }
    }
}
