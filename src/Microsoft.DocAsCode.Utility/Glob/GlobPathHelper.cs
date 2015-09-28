// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class GlobPathHelper
    {
        /// <summary>
        /// NOTE: '\' is considered as ESCAPE character, make sure to transform '\' in file path to '/' before globbing
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="globPattern"></param>
        /// <param name="filesProvider"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetFiles(
            string baseDirectory,
            string globPattern,
            Func<string, IEnumerable<string>> filesProvider = null)
        {
            if (string.IsNullOrEmpty(globPattern))
            {
                return Enumerable.Empty<string>();
            }
            // NOTE: '\' in base directory aslo need to be transformed
            return IronRuby.Builtins.Glob.GetMatches(baseDirectory.ToNormalizedPath(), globPattern, 0, filesProvider);
        }

        /// <summary>
        /// Convert glob pattern to regular expression
        /// </summary>
        /// <param name="pattern">The glob pattern</param>
        /// <param name="pathName">Specifies if the to-be-matched string is a path name</param>
        /// <param name="noEscape">Sepcifies whether or not to escape the pattern</param>
        /// <returns></returns>
        public static string GlobPatternToRegex(string pattern, bool pathName, bool noEscape)
        {
            return IronRuby.Builtins.Glob.PatternToRegex(pattern, pathName, noEscape);
        }
    }
}
