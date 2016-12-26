// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;
    using YamlSerialization;

    public class TreeItem
    {
        [YamlMember(Alias = "items")]
        public List<TreeItem> Items { get; set; }

        [ExtensibleMember]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
