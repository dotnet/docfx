// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class PlantUmlTest
{
    [Fact]
    public void TestRenderSvg_SequenceDiagram()
    {
        var source = """
                     ```plantuml
                     @startuml
                     Bob -> Alice : hello
                     @enduml
                     ```
                     """;

        var result = TestUtility.VerifyMarkup(source, html: null, plantUml: new()
        {
            OutputFormat = PlantUml.Net.OutputFormat.Svg,
            RemoteUrl = "https://www.plantuml.com/plantuml",
        }).TrimEnd();

        result.Should().StartWith("""<div class="lang-plantUml"><svg""");
        result.Should().EndWith("""hello</text><!--SRC=[SyfFKj2rKt3CoKnELR1Io4ZDoSa70000]--></g></svg></div>""");
    }
}
