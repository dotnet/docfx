// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class MathematicsTest
{
    [Fact]
    public void Test_Mathematics_Support_0()
    {
        var source = "$ math inline $";
        var expected = """<p><span class="math">\(math inline\)</span></p>""";

        TestUtility.VerifyMarkup(source, expected);
    }

    [Fact]
    public void Test_Mathematics_Support_1()
    {
        var source = "$ math^0 **inline** $";
        var expected = """<p><span class="math">\(math^0 **inline**\)</span></p>""";

        TestUtility.VerifyMarkup(source, expected);
    }
}
