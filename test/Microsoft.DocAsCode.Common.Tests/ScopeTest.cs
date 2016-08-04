// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;

    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "zhyan")]
    public class ScopeTest
    {
        [Fact]
        public void TestPhaseScope()
        {
            var listener = new TestLoggerListener();
            try
            {
                Logger.RegisterListener(listener);
                Action callback;

                Logger.LogInfo("test no phase scope");
                Assert.Null(listener.Items[0].Phase);

                using (new LoggerPhaseScope("A"))
                {
                    Logger.LogInfo("test in phase scope A");
                    Assert.Equal("A", listener.Items[1].Phase);

                    using (new LoggerPhaseScope("B"))
                    {
                        Logger.LogInfo("test in phase scope B");
                        Assert.Equal("A.B", listener.Items[2].Phase);

                        var captured = LoggerPhaseScope.Capture();
                        Assert.NotNull(captured);
                        callback = () =>
                        {
                            using (LoggerPhaseScope.Restore(captured))
                            {
                                Logger.LogInfo("test in captured phase scope B");
                            }
                        };
                    } // exit scope B.

                    using (new LoggerPhaseScope("C"))
                    {
                        Logger.LogInfo("test in phase scope C");
                        Assert.Equal("A.C", listener.Items[3].Phase);

                        // run callback in scope C.
                        callback();
                        Assert.Equal("A.B", listener.Items[4].Phase);
                    } // exit scope C.
                } // exit scope A.

                Logger.LogInfo("test no phase scope");
                Assert.Null(listener.Items[5].Phase);

                // run callback in no scope.
                callback();
                Assert.Equal("A.B", listener.Items[6].Phase);

                Logger.LogInfo("test no phase scope again");
                Assert.Null(listener.Items[7].Phase);
            }
            finally
            {
                Logger.UnregisterListener(listener);
            }
        }
    }
}
