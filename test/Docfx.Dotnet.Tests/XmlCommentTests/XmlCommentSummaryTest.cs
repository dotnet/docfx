// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentSummaryTest(ITestOutputHelper Output)
{
    private void ValidateSummary(string input, string expected)
    {
        // Act
        var result = XmlComment.Parse(input).Summary;

        // Assert
        result.Should().NotBeNull(); // Failed to get summary from XML input.
        try
        {
            result.Should()
                  .BeEquivalentTo(expected, x => x.IgnoringNewlineStyle());
        }
        catch
        {
            Output.WriteLine("Actual HTML:");
            Output.WriteLine("--------------------------------------------------------------------------------");
            Output.WriteLine(result);
            throw;
        }
    }
}
