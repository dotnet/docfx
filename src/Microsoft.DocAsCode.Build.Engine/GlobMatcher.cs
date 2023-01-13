// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Glob
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    [Serializable]
    public class GlobMatcher : IEquatable<GlobMatcher>
    {
        private readonly Glob _glob;
        public string Raw { get; }

        public GlobMatcher(string pattern)
        {
            pattern = PreProcessPattern(pattern);
            _glob = new Glob(pattern, GlobOptions.CaseInsensitive | GlobOptions.MatchFullPath | GlobOptions.Compiled);
        }

        public bool Match(string file, bool partial = false)
        {

        }

        private static string PreProcessPattern(string pattern)
        {
            // Pre process glob pattern so `**.md` means `**/*.md`
            // **** => **, **.md => **/*.md
            pattern = Regex.Replace(pattern, @"\*{2,}", "**");
            pattern = Regex.Replace(pattern, @"^\*{2}\.", "**/*.");
            pattern = Regex.Replace(pattern, @"\*\*\/\*$", "**");
            return pattern.Replace("/**.", "/**/*.").Replace("/**/**/", "/**/");
        }
    }
}
