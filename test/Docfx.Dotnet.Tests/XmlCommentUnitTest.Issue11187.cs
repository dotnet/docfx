// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentUnitTest
{
    [Fact]
    public void Issue11187()
    {
        // Act
        var result = XmlComment.Parse(
            """
            <member>
              <summary>
              Retrieves the most recent extended error code set by a WNet function
              <para/>P/Invoke call to mpr.dll - <seealso href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetgetlasterrora"/>
              </summary>
              <returns>If the function succeeds, and it obtains the last error that the network provider reported, the return value is NO_ERROR.<para/>If the caller supplies an invalid buffer, the return value is ERROR_INVALID_ADDRESS.</returns>
            </member>
            """);

        // Assert
        var summary = result.Summary;
        summary.Should().BeEquivalentTo(
            """
            Retrieves the most recent extended error code set by a WNet function
            
            <p></p>
            
            P/Invoke call to mpr.dll - <a href="https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetgetlasterrora">https://docs.microsoft.com/en-us/windows/desktop/api/winnetwk/nf-winnetwk-wnetgetlasterrora</a>
            """,
            options => options.IgnoringNewlineStyle());

        var returns = result.Returns;
        returns.Should().BeEquivalentTo(
            """
            If the function succeeds, and it obtains the last error that the network provider reported, the return value is NO_ERROR.

            <p></p>

            If the caller supplies an invalid buffer, the return value is ERROR_INVALID_ADDRESS.
            """,
            options => options.IgnoringNewlineStyle());
    }
}
