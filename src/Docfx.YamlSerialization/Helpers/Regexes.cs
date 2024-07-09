﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Docfx.YamlSerialization.Helpers;

internal static class Regexes
{
    // todo : boolean more for yaml http://yaml.org/type/bool.html
    // y|Y|yes|Yes|YES|n|N|no|No|NO
    // |true|True|TRUE|false|False|FALSE
    // |null|Null|NULL|~
    // |on|On|ON|off|Off|OFF
    public static readonly Regex BooleanLike = new("^(true|True|TRUE|false|False|FALSE)$", RegexOptions.Compiled);

    public static readonly Regex NullLike = new("^(null|Null|NULL|~)$", RegexOptions.Compiled);

    public static readonly Regex IntegerLike = new("^-?(0|[1-9][0-9]*)$", RegexOptions.Compiled);

    // https://yaml.org/spec/1.2/spec.html#id2805071
    public static readonly Regex FloatLike = new(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$", RegexOptions.Compiled);
}
