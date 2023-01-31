// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    public class MetadataModel
    {
        public MetadataItem TocYamlViewModel { get; set; }
        public List<MetadataItem> Members { get; set; }
    }
}
