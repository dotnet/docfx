// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.IO;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ConfigFilterRuleItemUnion
    {
        private ConfigFilterRuleIncludeItem _include;
        private ConfigFilterRuleExcludeItem _exclude;

        [YamlMember(Alias = "include")]
        public ConfigFilterRuleIncludeItem Include
        {
            get
            {
                return _include;
            }
            set
            {
                if (_exclude != null)
                {
                    throw new InvalidDataException("it is not allowed to specify both include and exclude in one rule");
                }
                _include = value;
            }
        }

        [YamlMember(Alias = "exclude")]
        public ConfigFilterRuleExcludeItem Exclude
        {
            get
            {
                return _exclude;
            }
            set
            {
                if (_include != null)
                {
                    throw new InvalidDataException("it is not allowed to specify both include and exclude in one rule");
                }
                _exclude = value;
            }
        }

        public ConfigFilterRuleItem Rule
        {
            get
            {
                if (Include != null)
                {
                    return Include;
                }
                return Exclude;
            }
        }
    }
}
