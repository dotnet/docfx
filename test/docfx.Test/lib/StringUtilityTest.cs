// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public static class StringUtilityTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("a", "a")]
    [InlineData("OUTPUT_PATH", "outputPath")]
    [InlineData("OUTPUT_PATH_", "outputPath")]
    [InlineData("_OUTPUT_PATH", "outputPath")]
    [InlineData("_OUTPUT_PATH_", "outputPath")]
    [InlineData("OUTPUT_PATH_NAME", "outputPathName")]
    public static void ToCamelCaseTest(string name, string camelCaseName)
    {
        Assert.Equal(camelCaseName, StringUtility.ToCamelCase('_', name));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("aBcD", "Abcd")]
    [InlineData("AbCd", "Abcd")]
    public static void UpperCaseFirstCharTest(string input, string expectedOutput)
    {
        Assert.Equal(expectedOutput, StringUtility.UpperCaseFirstChar(input));
    }
}
