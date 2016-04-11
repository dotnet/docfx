// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

    public class XRefMap
    {
        [YamlMember(Alias = "references")]
        public List<ReferenceViewModel> References { get; set; }

        [ExtensibleMember]
        public Dictionary<string, object> Others { get; set; } = new Dictionary<string, object>();
    }
}
