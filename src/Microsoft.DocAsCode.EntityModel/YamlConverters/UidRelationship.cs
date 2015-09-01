// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System.Collections.Generic;

    public sealed class UidRelationship
    {
        public UidRelationship(string uid)
        {
            Uid = uid;
        }

        public string Uid { get; private set; }

        public string Parent { get; set; }

        public List<string> Children { get; set; }

        public bool? IsPage { get; set; }
    }
}
