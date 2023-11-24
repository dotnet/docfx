// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

internal abstract class ConfigFilterRuleItem
{
    private Regex _uidRegex;

    [YamlMember(Alias = "uidRegex")]
    public string UidRegex
    {
        get
        {
            return _uidRegex?.ToString();
        }
        set
        {
            _uidRegex = new Regex(value);
        }
    }

    [YamlMember(Alias = "type")]
    public ExtendedSymbolKind? Kind { get; set; }

    [YamlMember(Alias = "hasAttribute")]
    public AttributeFilterInfo Attribute { get; set; }

    [YamlIgnore]
    public abstract bool CanVisit { get; }

    public bool IsMatch(SymbolFilterData symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var id = symbol.Id;

        return (_uidRegex == null || (id != null && _uidRegex.IsMatch(id))) &&
            (Kind == null || Kind.Value.Contains(symbol)) &&
            (Attribute == null || Attribute.ContainedIn(symbol));
    }
}
