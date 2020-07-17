using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation
{
    public static class ValidationHelper
    {
        public static string GetSkipPublishFilePath(string docsetFolder, string repoRootPath, string relativePath)
        {
            return Path.Combine(docsetFolder, relativePath).Replace(repoRootPath, "").TrimStart('\\').BackSlashToForwardSlash();
        }

        public static string GetLogItemFilePath(string docsetFolder, string repoRootPath, string relativePath)
        {
            return Path.Combine(docsetFolder, relativePath).Replace(repoRootPath, "").TrimStart('\\').ForwardSlashToBackSlash();
        }

        public static string BackSlashToForwardSlash(this string input)
        {
            return input?.Replace('/', '\\');
        }

        public static string ForwardSlashToBackSlash(this string input)
        {
            return input?.Replace('\\', '/');
        }
    }
}
