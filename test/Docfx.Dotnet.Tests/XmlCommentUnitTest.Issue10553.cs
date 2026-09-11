// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentUnitTest
{
    [Fact]
    public void Issue10553()
    {
        var result = XmlComment.Parse(
                """
                <summary>
                Converts action result without parameters into action result with null parameter.
                <example><code>
                <![CDATA[
                return NotFound() -> return NotFound(null)
                return NotFound() -> return NotFound(null)
                ]]></code>
                </example>
                This ensures our formatter is invoked, where we'll build a JSON:API compliant response. For details, see:
                https://github.com/dotnet/aspnetcore/issues/16969
                </summary>
                """);

        var expected = """
                Converts action result without parameters into action result with null parameter.

                <example>

                <pre><code class="lang-csharp">return NotFound() -&gt; return NotFound(null)
                return NotFound() -&gt; return NotFound(null)</code></pre>

                </example>

                This ensures our formatter is invoked, where we'll build a JSON:API compliant response. For details, see:
                https://github.com/dotnet/aspnetcore/issues/16969
                """.ReplaceLineEndings();

        // Verify empty line is inserted before/after `<example>` tags.
        result.Summary.Should().BeEquivalentTo(expected);
    }
}
