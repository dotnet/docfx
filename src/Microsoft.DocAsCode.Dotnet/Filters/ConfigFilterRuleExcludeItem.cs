// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Dotnet;

[Serializable]
internal class ConfigFilterRuleExcludeItem : ConfigFilterRuleItem
{
    [YamlIgnore]
    public override bool CanVisit
    {
        get
        {
            return false;
        }
    }
}
