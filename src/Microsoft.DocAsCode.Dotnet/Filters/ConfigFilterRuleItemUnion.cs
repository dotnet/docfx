// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Dotnet;

[Serializable]
internal class ConfigFilterRuleItemUnion
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
