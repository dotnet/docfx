// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class XmlCommentRemarksTest
{
    [Fact]
    public void Remarks()
    {
        ValidateRemarks(
            // Input XML
            """
            <remarks>
            <list type='bullet'>
                <item>
                    <description>
                        <code language = 'c#'>
                        public class XmlElement
                            : XmlLinkedNode
                        </code>
                    </description>
                </item>
            </list>
            </remarks>
            """,
            // Expected Markdown
            """
            <ul><li>
                        <pre><code class="lang-c#">public class XmlElement
                : XmlLinkedNode</code></pre>
                    </li></ul>
            """
         );
    }

    [Fact]
    public void Remarks_WithCodeBlocks()
    {
        ValidateRemarks(
            // Input XML
            """
            <remarks>
            ```csharp
            CSharpCode1
            ```

            <code>
            CSharpCode2
            </code>
            </remarks>
            """,
            // Expected Markdown
            """
            ```csharp
            CSharpCode1
            ```

            <pre><code class="lang-csharp">CSharpCode2</code></pre>
            """
         );
    }

    private static void ValidateRemarks(string input, string expected)
    {
        // Act
        var results = XmlComment.Parse(input).Remarks;

        // Assert
        results.Should().NotBeNull(); // Failed to get summary from XML input.

        results.Should()
               .BeEquivalentTo(expected, x => x.IgnoringNewlineStyle());
    }
}




