// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System.Collections.Generic;

    public sealed class YamlConverterContext : IHasUidIndex
    {
        public Dictionary<string, HashSet<FileAndType>> UidIndex { get; set; }

        public Dictionary<string, UidTreeNode> UidTree { get; set; }
    }
}
