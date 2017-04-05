// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;

    internal static class ParseTemplateHelper
    {
        private static readonly Regex IsRegexPatternRegex = new Regex(@"^\s*/(.*)/\s*$", RegexOptions.Compiled);
       
        /// <summary>
        /// file can start with "./" or using regex
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetResourceName(string file, string templateName, ResourceCollection resource)
        {
            if (string.IsNullOrWhiteSpace(file) || file == "./")
            {
                yield break;
            }

            if (file.StartsWith("./"))
            {
                file = file.Substring(2);
            }

            var regexPatternMatch = IsRegexPatternRegex.Match(file);
            if (regexPatternMatch.Groups.Count > 1)
            {
                file = regexPatternMatch.Groups[1].Value;
                var resourceKey = GetRelativeResourceKey(templateName, file);
                var regex = new Regex(resourceKey, RegexOptions.IgnoreCase);
                foreach (var name in resource.Names)
                {
                    if (regex.IsMatch(name))
                    {
                        yield return name;
                    }
                }
            }
            else
            {
                yield return GetRelativeResourceKey(templateName, file);
            }
        }

        private static string GetRelativeResourceKey(string templateName, string relativePath)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return relativePath;
            }

            // Make sure resource keys are combined using '/'
            return StringExtension.ForwardSlashCombine(StringExtension.ToNormalizedPath(Path.GetDirectoryName(templateName)), relativePath);
        }
    }
}
