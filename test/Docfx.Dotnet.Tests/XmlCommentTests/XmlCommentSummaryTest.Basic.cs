// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentSummaryTest
{
    [Fact]
    public void Summary_Basic()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            This is a simple summary.
            </summary>
            """,
            // Expected Markdown
            """
            This is a simple summary.
            """);
    }

    [Fact]
    public void Summary_WithMarkdown()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            This is a summary with **markdown**.
            </summary>
            """,
            // Expected Markdown
            "This is a summary with **markdown**.");
    }
}
