// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class XmlCommentTypeParametersTest
{
    [Fact]
    public void TypeParameters()
    {
        ValidateTypeParameters(
            // Input XML
            """
            <typeparam name="T">
            The base item type. Must implement IComparable.
            </typeparam>

            <typeparam name="T2">
            Exception: <see cref="T:System.Exception"/>.
            </typeparam>
            """,
            // Expected results
            new Dictionary<string, string>
            {
                ["T"] =
                """
                The base item type. Must implement IComparable.
                """,

                ["T2"] =
                """
                Exception: <xref href="System.Exception" data-throw-if-not-resolved="false"></xref>.
                """
            });
    }

    private static void ValidateTypeParameters(string input, Dictionary<string, string> expected)
    {
        // Act
        var results = XmlComment.Parse(input).TypeParameters;

        // Assert
        results.Should().NotBeNull(); // Failed to get summary from XML input.

        results.Should()
               .BeEquivalentTo(expected, x => x.IgnoringNewlineStyle());
    }
}




