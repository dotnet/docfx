// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Common.Tests;

[TestProperty("Related", "CollectionUtilityTest")]
[TestClass]
public class CollectionUtilityTest
{
    [TestMethod]
    [DataRow("ABCBDAB", "BDCABA", "BCBA")]
    [DataRow("ACCGGTCGAGTGCGCGGAAGCCGGCCGAA", "GTCGTTCGGAATGCCGTTGCTCTGTAAA", "GTCGTCGGAAGCCGGCCGAA")]
    public void TestGetLongestCommonSequence(string input1, string input2, string expected)
    {
        var inputArray = input1.ToImmutableArray();
        var actual = inputArray.GetLongestCommonSequence(input2.ToImmutableArray());
        Assert.AreEqual(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], actual[i]);
        }
    }
}
