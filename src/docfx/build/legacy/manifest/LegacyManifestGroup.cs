// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class LegacyManifestGroup
    {
        public string GroupId { get; set; }

        public List<string> Monikers { get; set; }
    }
}
