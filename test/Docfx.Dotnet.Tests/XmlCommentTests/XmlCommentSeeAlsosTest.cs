// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Docfx.DataContracts.ManagedReference;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class XmlCommentSeeAlsosTest
{
    [Fact]
    public void SeeAlsos()
    {
        ValidateSeeAlsos(
            // Input XML
            """
            <seealso cref="T:System.IO.WaitForChangedResult"/>
            <seealso cref="!:http://google.com">ABCS</seealso>
            <seealso href="http://www.bing.com">Hello Bing</seealso>
            <seealso href="http://www.bing.com"/>
            """,
            // Expected results
            [
                new LinkInfo
                {
                    LinkType = LinkType.CRef,
                    LinkId = "System.IO.WaitForChangedResult",
                    CommentId = "T:System.IO.WaitForChangedResult",
                    AltText = null,
                },
                new LinkInfo
                {
                    LinkType = LinkType.HRef,
                    LinkId = "http://www.bing.com",
                    CommentId = null,
                    AltText = "Hello Bing",
                },
                new LinkInfo
                {
                    LinkType = LinkType.HRef,
                    LinkId = "http://www.bing.com",
                    CommentId = null,
                    AltText = "http://www.bing.com",
                },
            ]);
    }

    private static void ValidateSeeAlsos(string input, LinkInfo[] expected)
    {
        // Act
        var results = XmlComment.Parse(input).SeeAlsos;

        // Assert
        results.Should().NotBeNull(); // Failed to get summary from XML input.

        results.Should()
               .BeEquivalentTo(expected, x => x.IgnoringNewlineStyle().WithStrictOrdering());
    }
}
