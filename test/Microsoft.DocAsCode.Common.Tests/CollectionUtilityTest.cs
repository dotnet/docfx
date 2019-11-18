// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    using Xunit;

    [Trait("Owner", "juchen")]
    [Trait("Related", "CollectionUtilityTest")]
    public class CollectionUtilityTest
    {
        [Theory]
        [InlineData("ABCBDAB", "BDCABA", "BCBA")]
        [InlineData("ACCGGTCGAGTGCGCGGAAGCCGGCCGAA", "GTCGTTCGGAATGCCGTTGCTCTGTAAA", "GTCGTCGGAAGCCGGCCGAA")]
        public void TestGetLongestCommonSequence(string input1, string input2, string expected)
        {
            var inputArray = input1.ToImmutableArray();
            var actual = inputArray.GetLongestCommonSequence(input2.ToImmutableArray());
            Assert.Equal(expected.Length, actual.Count());
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }
    }
}
