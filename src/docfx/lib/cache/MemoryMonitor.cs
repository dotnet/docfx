// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Docs.Build;

namespace System.Collections.Concurrent
{
    internal static class MemoryMonitor
    {
        private const int PollingInterval = 10 * 1000;
        private const int MemoryLimitPercentage = 90;

        private static readonly List<WeakReference<Action>> s_monitors = new List<WeakReference<Action>>();
        private static int s_lastGen2Count = 0;

        static MemoryMonitor()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                new Thread(MonitorMemory).Start();
            }
        }

        public static void AddMemoryMonitor(Action onMemoryLow)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                lock (s_monitors)
                {
                    s_monitors.Add(new WeakReference<Action>(onMemoryLow));
                }
            }
        }

        private static unsafe void MonitorMemory(object? obj)
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(PollingInterval);

                    // Ensure GC after last notification
                    var gen2Count = GC.CollectionCount(2);
                    if (gen2Count == s_lastGen2Count)
                    {
                        continue;
                    }
                    s_lastGen2Count = gen2Count;

                    MEMORYSTATUSEX memoryStatus = default;
                    memoryStatus.dwLength = (uint)sizeof(MEMORYSTATUSEX);

                    if (!GlobalMemoryStatusEx(ref memoryStatus))
                    {
                        continue;
                    }

                    if (memoryStatus.dwMemoryLoad < MemoryLimitPercentage)
                    {
                        continue;
                    }

                    Log.Important($"WARNING: Low memory {memoryStatus.dwMemoryLoad}", ConsoleColor.Red);

                    lock (s_monitors)
                    {
                        var cleanup = false;

                        foreach (var monitor in s_monitors)
                        {
                            if (monitor.TryGetTarget(out var action))
                            {
                                action();
                            }
                            else
                            {
                                cleanup = true;
                            }
                        }

                        if (cleanup)
                        {
                            s_monitors.RemoveAll(item => !item.TryGetTarget(out _));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Log.Write("Memory monitor failure:");
                    Log.Write(ex);
                }
                catch
                {
                }
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307", Justification = "Interop")]
        private struct MEMORYSTATUSEX
        {
            // The length field must be set to the size of this data structure.
            internal uint dwLength;
            internal uint dwMemoryLoad;
            internal ulong ullTotalPhys;
            internal ulong ullAvailPhys;
            internal ulong ullTotalPageFile;
            internal ulong ullAvailPageFile;
            internal ulong ullTotalVirtual;
            internal ulong ullAvailVirtual;
            internal ulong ullAvailExtendedVirtual;
        }
    }
}
