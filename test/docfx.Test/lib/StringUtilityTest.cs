// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public static class StringUtilityTest
    {
        [Theory]
        // zero if both of the string is null or empty
        [InlineData(null, null, 0)]
        [InlineData("", null, 0)]

        // if one of the string is null or empty, return the other string's length
        [InlineData("a", "", 1)]
        [InlineData("abc", null, 3)]

        // simple cases
        [InlineData("abc", "ab", 1)]
        [InlineData("one-two", "one", 4)]

        // complex cases
        [InlineData("hdinsight-hadoop-script-actions-linux", "hadoop", 31)]
        [InlineData("hdinsight-hadoop-script-actions-linux", "interactive-query", 28)]
        public static void LevenshteinDistance(string src, string target, int expectedDistance)
            => Assert.Equal(expectedDistance, Levenshtein.GetLevenshteinDistance(src, target));
    }
}
