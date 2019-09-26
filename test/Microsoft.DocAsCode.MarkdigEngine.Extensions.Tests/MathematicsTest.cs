// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class MathematicsTest
    {
        [Fact(Skip ="Disable math support")]
        public void Test_Mathematics_Support_0()
        {
            var source = "$ math inline $";
            var expected = @"<p><span class=""math"">math inline</span></p>";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact(Skip ="Disable math support")]
        public void Test_Mathematics_Support_1()
        {
            var source = "$ math^0 **inline** $";
            var expected = @"<p><span class=""math"">math^0 **inline**</span></p>";

            TestUtility.VerifyMarkup(source, expected);
        }
    }
}
