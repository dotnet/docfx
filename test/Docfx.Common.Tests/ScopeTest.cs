// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Common.Tests;

[Collection("docfx STA")]
public class ScopeTest
{
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

    [Fact]
    public void TestAggregatedPerformanceScope()
    {
        var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(null, LogLevel.Diagnostic);
        var logLevel = Logger.LogLevelThreshold;
        try
        {
            Logger.RegisterListener(listener);

            using (var aggregatedPerformanceScope = new AggregatedPerformanceScope(logLevel))
            using (new LoggerPhaseScope("A", logLevel, aggregatedPerformanceScope))
            {
                using (new LoggerPhaseScope("B", logLevel, aggregatedPerformanceScope))
                {
                } // exit scope B.

                for (int i = 0; i < 10; ++i)
                {
                    using (new LoggerPhaseScope("C", logLevel, aggregatedPerformanceScope))
                    {
                    } // exit scope C.
                }

                using (new LoggerPhaseScope("B"))
                {
                } // exit scope B.

                using (new LoggerPhaseScope("D"))
                {
                } // exit scope D.

                using (new LoggerPhaseScope("B", logLevel, aggregatedPerformanceScope))
                {
                    Parallel.ForEach(
                        Enumerable.Range(0, 100),
                        _ =>
                        {
                            using (new LoggerPhaseScope("E", logLevel, aggregatedPerformanceScope))
                            {
                                Thread.Sleep(10);
                            } // exit scope E.
                        });
                } // exit scope B.
            } // exit scope A.

            var allItems = listener.Items.ToList();
            Assert.Equal(117, allItems.Count);
            Assert.Equal(10, allItems.Count(item => item.Phase == "A.C"));
            Assert.Equal(100, allItems.Count(item => item.Phase == "A.B.E"));

            allItems.Reverse();

            var itemOfAC = TakeFirstLogItemAndRemove(allItems);
            Assert.Equal(Logger.LogLevelThreshold, itemOfAC.LogLevel);
            Assert.Contains("Phase 'A.C' runs 10 times with average time of", itemOfAC.Message);

            var itemOfABE = TakeFirstLogItemAndRemove(allItems);
            Assert.Equal(Logger.LogLevelThreshold, itemOfABE.LogLevel);
            Assert.Contains("Phase 'A.B.E' runs 100 times with average time of", itemOfABE.Message);

            var itemOfAB = TakeFirstLogItemAndRemove(allItems);
            Assert.Equal(Logger.LogLevelThreshold, itemOfAB.LogLevel);
            Assert.Contains("Phase 'A.B' runs 2 times with average time of", itemOfAB.Message);

            var itemOfA = TakeFirstLogItemAndRemove(allItems);
            Assert.Equal(Logger.LogLevelThreshold, itemOfA.LogLevel);
            Assert.Contains("Phase 'A' runs 1 times with average time of", itemOfA.Message);
        }
        finally
        {
            Logger.UnregisterListener(listener);
            Logger.LogLevelThreshold = logLevel;
        }
    }

    internal ILogItem TakeFirstLogItemAndRemove(List<ILogItem> items)
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
