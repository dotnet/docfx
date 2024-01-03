// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        var expected = """
                       <div class="lang-plantUml"><?xml version="1.0" encoding="us-ascii" standalone="no"?><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" contentStyleType="text/css" height="120px" preserveAspectRatio="none" style="width:113px;height:120px;background:#FFFFFF;" version="1.1" viewBox="0 0 113 120" width="113px" zoomAndPan="magnify"><defs/><g><line style="stroke:#181818;stroke-width:0.5;stroke-dasharray:5.0,5.0;" x1="26" x2="26" y1="36.2969" y2="85.4297"/><line style="stroke:#181818;stroke-width:0.5;stroke-dasharray:5.0,5.0;" x1="82" x2="82" y1="36.2969" y2="85.4297"/><rect fill="#E2E2F0" height="30.2969" rx="2.5" ry="2.5" style="stroke:#181818;stroke-width:0.5;" width="43" x="5" y="5"/><text fill="#000000" font-family="sans-serif" font-size="14" lengthAdjust="spacing" textLength="29" x="12" y="24.9951">Bob</text><rect fill="#E2E2F0" height="30.2969" rx="2.5" ry="2.5" style="stroke:#181818;stroke-width:0.5;" width="43" x="5" y="84.4297"/><text fill="#000000" font-family="sans-serif" font-size="14" lengthAdjust="spacing" textLength="29" x="12" y="104.4248">Bob</text><rect fill="#E2E2F0" height="30.2969" rx="2.5" ry="2.5" style="stroke:#181818;stroke-width:0.5;" width="49" x="58" y="5"/><text fill="#000000" font-family="sans-serif" font-size="14" lengthAdjust="spacing" textLength="35" x="65" y="24.9951">Alice</text><rect fill="#E2E2F0" height="30.2969" rx="2.5" ry="2.5" style="stroke:#181818;stroke-width:0.5;" width="49" x="58" y="84.4297"/><text fill="#000000" font-family="sans-serif" font-size="14" lengthAdjust="spacing" textLength="35" x="65" y="104.4248">Alice</text><polygon fill="#181818" points="70.5,63.4297,80.5,67.4297,70.5,71.4297,74.5,67.4297" style="stroke:#181818;stroke-width:1.0;"/><line style="stroke:#181818;stroke-width:1.0;" x1="26.5" x2="76.5" y1="67.4297" y2="67.4297"/><text fill="#000000" font-family="sans-serif" font-size="13" lengthAdjust="spacing" textLength="30" x="33.5" y="62.3638">hello</text><!--SRC=[SyfFKj2rKt3CoKnELR1Io4ZDoSa70000]--></g></svg></div>
                       """;

        TestUtility.VerifyMarkup(source, expected, extensionConfiguration:
            new Dictionary<string, string>
            {
                { "outputFormat", "svg"},
                { "remoteUrl", "https://www.plantuml.com/plantuml" }
            });
    }
}
