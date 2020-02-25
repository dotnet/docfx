// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class StringUtility
    {
        public static string ToCamelCase(char wordSeparator, string value)
        {
            var sb = new StringBuilder();
            var words = value.ToLowerInvariant().Split(wordSeparator);
            sb.Length = 0;
            sb.Append(words[0]);
            for (var i = 1; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(words[i][0]));
                    sb.Append(words[i], 1, words[i].Length - 1);
                }
            }
            return sb.ToString().Trim();
        }
    }
}
