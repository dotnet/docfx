// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Docfx.DataContracts.ManagedReference;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class XmlCommentExceptionsTest
{
    [Fact]
    public void Exceptions()
    {
        ValidateException(
            // Input XML
            """
            <exception cref="T:System.Xml.XmlException">This is a sample of exception node. Ref <see href="http://exception.com">Exception</see></exception>
            <exception cref="T:Docfx.Exceptions.DocfxException">DocfxException</exception>
            <!-- Folloiwng items are ignored -->
            <exception cref="System.Xml.XmlException">This is a sample of exception node with invalid cref</exception>
            <exception cref="">This is a sample of invalid exception node</exception>
            <exception>This is a sample of another invalid exception node</exception>
            """,
            // Expected results
            [
                new ExceptionInfo
                {
                    Type = "System.Xml.XmlException",
                    CommentId = "T:System.Xml.XmlException",
                    Description =
                    """
                    This is a sample of exception node. Ref <a href="http://exception.com">Exception</a>
                    """,
                },
                new ExceptionInfo
                {
                    Type = "Docfx.Exceptions.DocfxException",
                    CommentId = "T:Docfx.Exceptions.DocfxException",
                    Description = "DocfxException",
                },
                // Other exception tags are ignored. Because it's invalid.
            ]);
    }

    private static void ValidateException(string input, ExceptionInfo[] expected)
    {
        // Act
        var results = XmlComment.Parse(input).Exceptions;

        // Assert
        results.Should().NotBeNull(); // Failed to get summary from XML input.

        results.Should()
               .BeEquivalentTo(expected, x => x.IgnoringNewlineStyle().WithStrictOrdering());
    }
}




