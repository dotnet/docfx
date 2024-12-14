// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

internal class AttributeFilterInfo
{
    [YamlMember(Alias = "uid")]
    public string Uid { get; set; }

    [YamlMember(Alias = "ctorArguments")]
    public List<string> ConstructorArguments { get; set; }

    [YamlMember(Alias = "ctorNamedArguments")]
    public Dictionary<string, string> ConstructorNamedArguments { get; set; } = [];

    public bool ContainedIn(SymbolFilterData symbol)
    {
        bool result = false;
        var attributes = symbol.Attributes;
        foreach (var attribute in attributes)
        {
            if (Uid != attribute.Id)
            {
                continue;
            }

            // arguments need to be a total match of the config
            if (ConstructorArguments != null && !ConstructorArguments.SequenceEqual(attribute.ConstructorArguments))
            {
                continue;
            }

            // namedarguments need to be a superset of the config
            if (!ConstructorNamedArguments.Except(attribute.ConstructorNamedArguments).Any())
            {
                result = true;
                break;
            }
        }

        return result;
    }
}
