// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tests.Common;

    [Trait("Owner", "vwxyzh")]
    [Collection("docfx STA")]
    public class ScopeTest
    {
        [Fact]
        public void TestPhaseScope()
        {
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualMatcher(null, LogLevel.Diagnostic);
            var logLevel = Logger.LogLevelThreshold;
            ILogItem item;
            try
            {
                Logger.LogLevelThreshold = LogLevel.Diagnostic;
                Logger.RegisterListener(listener);
                Action<bool> callback;

                Logger.LogInfo("test no phase scope");
                Assert.Null(listener.TakeAndRemove().Phase);

                using (new LoggerPhaseScope("A"))
                {
                    Logger.LogInfo("test in phase scope A");
                    Assert.Equal("A", listener.TakeAndRemove().Phase);

                    using (new LoggerPhaseScope("B"))
                    {
                        Logger.LogInfo("test in phase scope B");
                        Assert.Equal("A.B", listener.TakeAndRemove().Phase);

                        var captured = LoggerPhaseScope.Capture();
                        Assert.NotNull(captured);
                        callback = shouldLogPerformance =>
                        {
                            using (shouldLogPerformance ?
                                LoggerPhaseScope.Restore(captured, LogLevel.Diagnostic) :
                                LoggerPhaseScope.Restore(captured))
                            {
                                Logger.LogInfo("test in captured phase scope B");
                            }
                        };
                    } // exit scope B.

                    using (new LoggerPhaseScope("C", LogLevel.Diagnostic))
                    {
                        Logger.LogInfo("test in phase scope C");
                        Assert.Equal("A.C", listener.TakeAndRemove().Phase);

                        // run callback in scope C.
                        callback(false);
                        Assert.Equal("A.B", listener.TakeAndRemove().Phase);
                    } // exit scope C.

                    item = listener.TakeAndRemove();
                    Assert.Equal("A.C", item.Phase);
                    Assert.Equal(LogLevel.Diagnostic, item.LogLevel);
                } // exit scope A.

                Logger.LogInfo("test no phase scope");
                Assert.Null(listener.TakeAndRemove().Phase);

                // run callback in no scope.
                callback(true);
                Assert.Equal("A.B", listener.TakeAndRemove().Phase);
                item = listener.TakeAndRemove();
                Assert.Equal("A.B", item.Phase);
                Assert.Equal(LogLevel.Diagnostic, item.LogLevel);

                Logger.LogInfo("test no phase scope again");
                Assert.Null(listener.TakeAndRemove().Phase);
            }
            finally
            {
                Logger.UnregisterListener(listener);
                Logger.LogLevelThreshold = logLevel;
            }
        }

        [Fact]
        public void TestFileScope()
        {
            const string PhaseName = "TestFileScope";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualMatcher(PhaseName, LogLevel.Info);
            try
            {
                Logger.RegisterListener(listener);
                Action callback;

                Logger.LogInfo("Not in file scope.", PhaseName);
                Assert.Null(listener.TakeAndRemove().File);

                using (new LoggerFileScope("A"))
                {
                    Logger.LogInfo("In file A", PhaseName);
                    Assert.Equal("A", listener.TakeAndRemove().File);

                    using (new LoggerFileScope("B"))
                    {
                        Logger.LogInfo("In file B", PhaseName);
                        Assert.Equal("B", listener.TakeAndRemove().File);

                        var captured = LoggerFileScope.Capture();
                        callback = () =>
                        {
                            using (LoggerFileScope.Restore(captured))
                            {
                                Logger.LogInfo("In captured file B", PhaseName);
                            }
                        };
                    }

                    Logger.LogInfo("In file A", PhaseName);
                    Assert.Equal("A", listener.TakeAndRemove().File);

                    callback();
                    Assert.Equal("B", listener.TakeAndRemove().File);

                    Logger.LogInfo("In file A", PhaseName);
                    Assert.Equal("A", listener.TakeAndRemove().File);

                    using (new LoggerFileScope("C"))
                    {
                        Logger.LogInfo("In file C", PhaseName);
                        Assert.Equal("C", listener.TakeAndRemove().File);

                        callback();
                        Assert.Equal("B", listener.TakeAndRemove().File);
                    }
                }

                Logger.LogInfo("Not in file scope.", PhaseName);
                Assert.Null(listener.TakeAndRemove().File);

                callback();
                Assert.Equal("B", listener.TakeAndRemove().File);

                Logger.LogInfo("Not in file scope.", PhaseName);
                Assert.Null(listener.TakeAndRemove().File);
            }
            finally
            {
                Logger.UnregisterListener(listener);
            }
        }
    }
}
