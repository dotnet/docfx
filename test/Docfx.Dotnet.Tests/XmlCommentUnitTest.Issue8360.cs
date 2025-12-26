// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Docfx.DataContracts.ManagedReference;
using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentUnitTest
{
    [Fact]
    public void Issue8360()
    {
        // Act
        var result = XmlComment.Parse(
                """
                <summary>
                Test <see cref="T:InvalidOperationException" />.
                </summary>
                <exception cref="T:InvalidOperationException">InvalidOperationException</exception>
                <exception cref="T:ArgumentNullException">ArgumentNullException</exception>
                """);

        // Assert
        var summary = result.Summary;
        summary.Should().Be(
                """
                Test <xref href="InvalidOperationException" data-throw-if-not-resolved="false"></xref>.
                """);

        var exceptions = result.Exceptions;
        exceptions.Should().BeEquivalentTo(
            [
                new ExceptionInfo
                {
                    Type ="InvalidOperationException",
                    CommentId ="T:InvalidOperationException",
                    Description="InvalidOperationException",
                },
                 new ExceptionInfo
                {
                    Type ="ArgumentNullException",
                    CommentId ="T:ArgumentNullException",
                    Description="ArgumentNullException",
                },
            ],
            options => options.WithStrictOrdering());
    }
}
