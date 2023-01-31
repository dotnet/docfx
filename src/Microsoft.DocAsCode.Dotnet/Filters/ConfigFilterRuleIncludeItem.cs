// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    using YamlDotNet.Serialization;

    [Serializable]
    public class ConfigFilterRuleIncludeItem : ConfigFilterRuleItem
    {
        [YamlIgnore]
        public override bool CanVisit
        {
            get
            {
                return true;
            }
        }
    }
}
