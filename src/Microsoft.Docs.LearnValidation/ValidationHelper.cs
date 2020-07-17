// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

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
