// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

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
