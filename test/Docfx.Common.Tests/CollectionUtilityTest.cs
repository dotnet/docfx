// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Xunit;

namespace Docfx.Common.Tests;

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
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }
}
