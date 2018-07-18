// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Microsoft.Docs.Build.TestFramework", "docfx.Test")]

namespace Microsoft.Docs.Build
{
    public class TestFramework : XunitTestFramework
    {
        public TestFramework(IMessageSink messageSink)
            : base(messageSink)
        { }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            Environment.SetEnvironmentVariable("DOCFX_APPDATA_PATH", Path.GetFullPath("app-data"));
            MakeDebugAssertThrowException();
            return new ParallelExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }

        private static void MakeDebugAssertThrowException()
        {
            // This only works for .NET core
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Diagnostics/Debug.cs
            // https://github.com/dotnet/corefx/blob/8dbeee99ce48a46c3cee9d1b765c3b31af94e172/src/System.Diagnostics.Debug/tests/DebugTests.cs
            var showDialogHook = typeof(Debug).GetField("s_ShowDialog", BindingFlags.Static | BindingFlags.NonPublic);
            showDialogHook?.SetValue(null, new Action<string, string, string, string>(Throw));

            void Throw(string stackTrace, string message, string detailMessage, string info)
            {
                Assert.True(false, $"Debug.Assert failed: {message} {detailMessage}\n{stackTrace}");
            }
        }

        class ParallelExecutor : XunitTestFrameworkExecutor
        {
            public ParallelExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
                : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
            { }

            protected async override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            {
                using (var assemblyRunner = new ParallelAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions))
                {
                    await assemblyRunner.RunAsync();
                }
            }
        }

        class ParallelAssemblyRunner : XunitTestAssemblyRunner
        {
            public ParallelAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
                : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
            { }

            protected async override Task<RunSummary> RunTestCollectionsAsync(IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
            {
                var summary = new RunSummary();
                var times = new ConcurrentBag<decimal>();

                await ParallelUtility.ForEach(TestCases, async testCase =>
                {
                    var runSummary = await base.RunTestCollectionAsync(messageBus, testCase.TestMethod.TestClass.TestCollection, new[] { testCase }, cancellationTokenSource);
                    Interlocked.Add(ref summary.Total, runSummary.Total);
                    Interlocked.Add(ref summary.Failed, runSummary.Failed);
                    Interlocked.Add(ref summary.Skipped, runSummary.Skipped);
                    times.Add(runSummary.Time);
                });

                summary.Time = times.Max();
                return summary;
            }
        }
    }

    public static class TestFrameworkTest
    {
        [Fact]
        public static void DebugAssertThrowsException()
        {
#if DEBUG
            Assert.ThrowsAny<Exception>(() => Debug.Assert(false));
#endif
        }
    }
}
