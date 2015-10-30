// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;

    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Serializable]
    public class ApiParameter
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "id")]
        [MergeOption(MergeOption.MergeKey)]
        public string Name { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
