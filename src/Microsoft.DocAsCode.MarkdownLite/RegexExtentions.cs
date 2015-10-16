// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    internal static class RegexExtentions
    {
        public static string NotEmpty(this Match match, int index1, int index2)
        {
            if (match.Groups.Count > index1 && !string.IsNullOrEmpty(match.Groups[index1].Value))
            {
                return match.Groups[index1].Value;
            }
            return match.Groups[index2].Value;
        }
    }
}
