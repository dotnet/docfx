using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Microsoft.Docs.Build.Analyzers.Test.CSharpAnalyzerVerifier<
    Microsoft.Docs.Build.Analyzers.BuildErrorsAnalyzer>;

namespace Microsoft.Docs.Build.Analyzers.Test
{
    [TestClass]
    public class BuildErrorsAnalyzersUnitTests
    {
        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class ABC
        {
            public Error GetError()
            {
                FormattableString message = $""abce"";
                return new Error(""code"", {|#0:message|});
            }
    }

        internal class Error
        {
            private string v1;
            private FormattableString v2;

            public Error(string v1, FormattableString v2)
            {
                this.v1 = v1;
                this.v2 = v2;
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("BuildErrorsAnalyzer").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
