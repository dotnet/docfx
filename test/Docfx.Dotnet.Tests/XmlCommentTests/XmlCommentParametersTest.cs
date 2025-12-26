// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class XmlCommentParametersTest
{
    [Fact]
    public void Parameters()
    {
        ValidateParameters(
            // Input XML
            """
            <param name="input">This is <see cref='T:System.AccessViolationException'/>the input</param>
            <param name="output">This is the output</param >
            """,
            // Expected results
            new Dictionary<string, string>
            {
                ["input"] =
                """
                This is <xref href="System.AccessViolationException" data-throw-if-not-resolved="false"></xref>the input
                """,

                ["output"] =
                """
                This is the output
                """,
            });
    }

    private static void ValidateParameters(string input, Dictionary<string, string> expected)
    {
        // Act
        var results = XmlComment.Parse(input).Parameters;

        // Assert
        results.Should().NotBeNull(); // Failed to get summary from XML input.

        results.Should()
               .BeEquivalentTo(expected, x => x.IgnoringNewlineStyle().WithStrictOrdering());
    }
}




