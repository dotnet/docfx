// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class OverwriteConfigIdentifer
    {
        private static Regex s_branchRegex = new Regex(@"branches:[ ]*\[(.+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_localeRegex = new Regex(@"locales:[ ]*\[(.+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public OverwriteConfigIdentifer(string identifierStr)
        {
            Debug.Assert(!string.IsNullOrEmpty(identifierStr));
            Branches = new HashSet<string>(GetMatchParts(s_branchRegex), StringComparer.OrdinalIgnoreCase);
            Locales = new HashSet<string>(GetMatchParts(s_localeRegex), StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> GetMatchParts(Regex regex)
            {
                var match = regex.Match(identifierStr);
                if (match.Success)
                {
                    return match.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());
                }

                return Array.Empty<string>();
            }
        }

        public HashSet<string> Locales { get; }

        public HashSet<string> Branches { get; }

        public static bool Match(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            return s_branchRegex.Match(str).Success || s_localeRegex.Match(str).Success;
        }
    }
}
