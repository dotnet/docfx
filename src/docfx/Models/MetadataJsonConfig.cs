// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    public class MetadataJsonConfig : List<MetadataJsonItemConfig>
    {
        public MetadataJsonConfig(IEnumerable<MetadataJsonItemConfig> configs) : base(configs) { }

        public MetadataJsonConfig(params MetadataJsonItemConfig[] configs) : base(configs)
        {
        }
    }
}
