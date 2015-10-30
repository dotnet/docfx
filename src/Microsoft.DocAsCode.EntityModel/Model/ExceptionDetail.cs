// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Serializable]
    public class CrefInfo
    {
        [YamlMember(Alias = "type")]
        [MergeOption(MergeOption.MergeKey)]
        public string Type { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
