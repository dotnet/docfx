// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;

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
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(null, LogLevel.Diagnostic);
            var logLevel = Logger.LogLevelThreshold;
            ILogItem item;
            try
            {
                Logger.LogLevelThreshold = LogLevel.Diagnostic;
                Logger.RegisterListener(listener);
                Action<bool> callback;

                Logger.LogInfo("test no phase scope");
                Assert.Null(TakeFirstLogItemAndRemove(listener.Items).Phase);

                using (new LoggerPhaseScope("A"))
                {
                    Logger.LogInfo("test in phase scope A");
                    Assert.Equal("A", TakeFirstLogItemAndRemove(listener.Items).Phase);

                    using (new LoggerPhaseScope("B"))
                    {
                        Logger.LogInfo("test in phase scope B");
                        Assert.Equal("A.B", TakeFirstLogItemAndRemove(listener.Items).Phase);

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
                        Assert.Equal("A.C", TakeFirstLogItemAndRemove(listener.Items).Phase);

                        // run callback in scope C.
                        callback(false);
                        Assert.Equal("A.B", TakeFirstLogItemAndRemove(listener.Items).Phase);
                    } // exit scope C.

                    item = TakeFirstLogItemAndRemove(listener.Items);
                    Assert.Equal("A.C", item.Phase);
                    Assert.Equal(LogLevel.Diagnostic, item.LogLevel);
                } // exit scope A.

                Logger.LogInfo("test no phase scope");
                Assert.Null(TakeFirstLogItemAndRemove(listener.Items).Phase);

                // run callback in no scope.
                callback(true);
                Assert.Equal("A.B", TakeFirstLogItemAndRemove(listener.Items).Phase);
                item = TakeFirstLogItemAndRemove(listener.Items);
                Assert.Equal("A.B", item.Phase);
                Assert.Equal(LogLevel.Diagnostic, item.LogLevel);

                Logger.LogInfo("test no phase scope again");
                Assert.Null(TakeFirstLogItemAndRemove(listener.Items).Phase);
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
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(PhaseName, LogLevel.Info);
            try
            {
                Logger.RegisterListener(listener);
                Action callback;

                Logger.LogInfo("Not in file scope.", PhaseName);
                Assert.Null(TakeFirstLogItemAndRemove(listener.Items).File);

                using (new LoggerFileScope("A"))
                {
                    Logger.LogInfo("In file A", PhaseName);
                    Assert.Equal("A", TakeFirstLogItemAndRemove(listener.Items).File);

                    using (new LoggerFileScope("B"))
                    {
                        Logger.LogInfo("In file B", PhaseName);
                        Assert.Equal("B", TakeFirstLogItemAndRemove(listener.Items).File);

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
                    Assert.Equal("A", TakeFirstLogItemAndRemove(listener.Items).File);

                    callback();
                    Assert.Equal("B", TakeFirstLogItemAndRemove(listener.Items).File);

                    Logger.LogInfo("In file A", PhaseName);
                    Assert.Equal("A", TakeFirstLogItemAndRemove(listener.Items).File);

                    using (new LoggerFileScope("C"))
                    {
                        Logger.LogInfo("In file C", PhaseName);
                        Assert.Equal("C", TakeFirstLogItemAndRemove(listener.Items).File);

                        callback();
                        Assert.Equal("B", TakeFirstLogItemAndRemove(listener.Items).File);
                    }
                }

                Logger.LogInfo("Not in file scope.", PhaseName);
                Assert.Null(TakeFirstLogItemAndRemove(listener.Items).File);

                callback();
                Assert.Equal("B", TakeFirstLogItemAndRemove(listener.Items).File);

                Logger.LogInfo("Not in file scope.", PhaseName);
                Assert.Null(TakeFirstLogItemAndRemove(listener.Items).File);
            }
            finally
            {
                Logger.UnregisterListener(listener);
            }
        }

        public ILogItem TakeFirstLogItemAndRemove(List<ILogItem> items)
        {
            if (items.Count == 0)
            {
                return null;
            }
            var result = items[0];
            items.RemoveAt(0);
            return result;
        }
    }
}
