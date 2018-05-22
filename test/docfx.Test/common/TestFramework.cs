// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection;
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
            MakeDebugAssertThrowException();
            return base.CreateExecutor(assemblyName);
        }

        private static void MakeDebugAssertThrowException()
        {
            // This only works for .NET core
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Diagnostics/Debug.cs
            // https://github.com/dotnet/corefx/blob/8dbeee99ce48a46c3cee9d1b765c3b31af94e172/src/System.Diagnostics.Debug/tests/DebugTests.cs
            var showDialogHook = typeof(Debug).GetField("s_ShowAssertDialog", BindingFlags.Static | BindingFlags.NonPublic);
            showDialogHook?.SetValue(null, new Action<string, string, string>(Throw));

            void Throw(string stackTrace, string message, string detailMessage)
            {
                Assert.True(false, $"Debug.Assert failed: {message} {detailMessage}\n{stackTrace}");
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
