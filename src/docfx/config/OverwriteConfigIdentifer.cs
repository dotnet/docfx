// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class OverwriteConfigIdentifier
    {
        private static Regex s_branchRegex = new Regex(@"branches:[ ]*\[([^\[\]]*)\]", RegexOptions.IgnoreCase);
        private static Regex s_localeRegex = new Regex(@"locales:[ ]*\[([^\[\]]*)\]", RegexOptions.IgnoreCase);

        private OverwriteConfigIdentifier(HashSet<string> branches, HashSet<string> locales)
        {
            Branches = branches;
            Locales = locales;
        }

        public static bool TryMatch(string identifierStr, out OverwriteConfigIdentifier overwriteConfigIdentifier)
        {
            Debug.Assert(!string.IsNullOrEmpty(identifierStr));

            overwriteConfigIdentifier = null;
            var branchMatched = TryGetMatchedParts(s_branchRegex, out var branches);
            var localeMatched = TryGetMatchedParts(s_localeRegex, out var locales);
            if (branchMatched || localeMatched)
            {
                overwriteConfigIdentifier = new OverwriteConfigIdentifier(new HashSet<string>(branches), new HashSet<string>(locales, StringComparer.OrdinalIgnoreCase));
                return true;
            }

            return false;

            bool TryGetMatchedParts(Regex regex, out IEnumerable<string> parts)
            {
                parts = Array.Empty<string>();
                var match = regex.Match(identifierStr);
                if (match.Success)
                {
                    parts = match.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());
                    return true;
                }

                return false;
            }
        }

        public HashSet<string> Locales { get; }

        public HashSet<string> Branches { get; }
    }
}
