// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

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

    [YamlIgnore]
    public ConfigFilterRuleItem Rule
    {
        get
        {
            if (Include != null)
            {
                return Include;
            }

            // If kind is not specified for exclude. Set `ExtendedSymbolKind.Type` as default kind.
            if (Exclude != null)
            {
                Exclude.Kind ??= ExtendedSymbolKind.Type | ExtendedSymbolKind.Member;
            }

            return Exclude;
        }
    }
}
