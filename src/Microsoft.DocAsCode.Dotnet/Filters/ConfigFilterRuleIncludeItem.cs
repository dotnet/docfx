// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Dotnet;

[Serializable]
internal class ConfigFilterRuleIncludeItem : ConfigFilterRuleItem
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
