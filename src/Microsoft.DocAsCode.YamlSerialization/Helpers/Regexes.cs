// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.Helpers
{
    using System.Text.RegularExpressions;

    internal static class Regexes
    {
        public static readonly Regex BooleanLike = new Regex(@"^(true|false)$", RegexOptions.Compiled);
        public static readonly Regex IntegerLike = new Regex(@"^-?(0|[1-9][0-9]*)$", RegexOptions.Compiled);
        public static readonly Regex DoubleLike = new Regex(@"^-?(0|[1-9][0-9]*)(\.[0-9]*)?([eE][-+]?[0-9]+)?$", RegexOptions.Compiled);
    }
}
