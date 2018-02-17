// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ConfigFilterRule
    {
        [YamlMember(Alias = "apiRules")]
        public List<ConfigFilterRuleItemUnion> ApiRules { get; set; } = new List<ConfigFilterRuleItemUnion>();

        [YamlMember(Alias = "attributeRules")]
        public List<ConfigFilterRuleItemUnion> AttributeRules { get; set; } = new List<ConfigFilterRuleItemUnion>();
    }
}
