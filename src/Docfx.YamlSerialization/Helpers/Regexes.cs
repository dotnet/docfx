// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Docfx.YamlSerialization.Helpers;

internal static partial class Regexes
{
    // todo : boolean more for yaml http://yaml.org/type/bool.html
    // y|Y|yes|Yes|YES|n|N|no|No|NO
    // |true|True|TRUE|false|False|FALSE
    // |null|Null|NULL|~
    // |on|On|ON|off|Off|OFF
    [GeneratedRegex("^(true|True|TRUE|false|False|FALSE)$")]
    public static partial Regex BooleanLike();

    [GeneratedRegex("^(null|Null|NULL|~)$")]
    public static partial Regex NullLike();

    [GeneratedRegex("^-?(0|[1-9][0-9]*)$")]
    public static partial Regex IntegerLike();

    // https://yaml.org/spec/1.2/spec.html#id2805071
    [GeneratedRegex(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$")]
    public static partial Regex FloatLike();
}
