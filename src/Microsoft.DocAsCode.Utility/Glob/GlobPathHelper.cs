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
    }
}
