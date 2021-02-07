using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
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
                return new Error(ErrorLevel.Warning, ""code"", {|#0:message|});
            }
            public Error GetError2(bool warning)
            {
                return new Error( {|#1:warning ? ErrorLevel.Warning : ErrorLevel.Error|}, ""code"", $""abce"");
            }
            public Error GetErrorCode(string warning)
            {
                return new Error( ErrorLevel.Warning, {|#2:warning|}, $""abce"");
            }
            public Error GetErrorCode2()
            {
                var code = ""abc"";
                return new Error( ErrorLevel.Warning, {|#21:code|}, $""abce"");
            }
            public Error GetError3(string url)
            {
                return new Error(ErrorLevel.Error, ""code"", {|#30:url == ""ok"" ? $""failed{url}"" : $""success""|});
            }
            public Error GetErrorAll(bool warning)
            {
                var code = ""abc"";
                return new Error({|#41:warning ? ErrorLevel.Warning : ErrorLevel.Error|}, {|#42:code|}, $""abce"");
            }
        }

        internal class Error
        {
            public Error(ErrorLevel level, string v1, FormattableString v2)
            {
            }
        }

        internal enum ErrorLevel
        {
            Off,
            Info,
            Suggestion,
            Warning,
            Error,
        }
    }";

            var expected = new DiagnosticResult[]
            {
                VerifyCS.Diagnostic(BuildErrorsAnalyzer.ShouldBeInterpolatedStringRule).WithLocation(0),
                VerifyCS.Diagnostic(BuildErrorsAnalyzer.ShouldBeMemberAccessExpressionRule).WithLocation(1),
                VerifyCS.Diagnostic(BuildErrorsAnalyzer.ShouldBePlainStringRule).WithLocation(2),
                VerifyCS.Diagnostic(BuildErrorsAnalyzer.ShouldBePlainStringRule).WithLocation(21),
                VerifyCS.Diagnostic(BuildErrorsAnalyzer.ShouldBeInterpolatedStringRule).WithLocation(30),
                DiagnosticResult.CompilerError("CS1503").WithLocation(30).WithArguments("3", "string", "System.FormattableString"), // error of language version 8: cannot convert from 'string' to 'System.FormattableString'
                VerifyCS.Diagnostic(BuildErrorsAnalyzer.ShouldBeMemberAccessExpressionRule).WithLocation(41),
                VerifyCS.Diagnostic(BuildErrorsAnalyzer.ShouldBePlainStringRule).WithLocation(42),
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
