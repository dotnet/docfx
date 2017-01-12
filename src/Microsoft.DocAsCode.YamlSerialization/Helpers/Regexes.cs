﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.Helpers
{
    using System.Text.RegularExpressions;

    internal static class Regexes
    {
        private const RegexOptions RegexOptionsCompiled =
#if NetCore
            RegexOptions.None;
#else
            RegexOptions.Compiled;
#endif
        // todo : boolean more for yaml http://yaml.org/type/bool.html
        // y|Y|yes|Yes|YES|n|N|no|No|NO
        // |true|True|TRUE|false|False|FALSE
        // |null|Null|NULL
        // |on|On|ON|off|Off|OFF
        public static readonly Regex BooleanLike = new Regex(@"^(true|True|TRUE|false|False|FALSE)$", RegexOptionsCompiled);
        public static readonly Regex NullLike = new Regex(@"^(null|Null|NULL)$", RegexOptionsCompiled);
        public static readonly Regex IntegerLike = new Regex(@"^-?(0|[1-9][0-9]*)$", RegexOptionsCompiled);
        public static readonly Regex DoubleLike = new Regex(@"^-?(0|[1-9][0-9]*)(\.[0-9]*)?([eE][-+]?[0-9]+)?$", RegexOptionsCompiled);
    }
}
