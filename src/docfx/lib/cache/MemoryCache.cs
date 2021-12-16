// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Docs.Build;

namespace System.Collections.Concurrent;

internal static class MemoryCache
{
    private const int PollingInterval = 10 * 1000;
    private const int MemoryLimitPercentage = 70;

    private static readonly List<WeakReference<IMemoryCache>> s_caches = new();
    private static int s_lastGen2Count;

    static MemoryCache()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            new Thread(MonitorMemory) { IsBackground = true }.Start();
        }
    }

    public static void Clear()
    {
        ForEach(cache => cache.Clear());
    }

    public static void AddMemoryMonitor(IMemoryCache monitor)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            lock (s_caches)
            {
                s_caches.Add(new WeakReference<IMemoryCache>(monitor));
            }
        }
    }

    private static unsafe void MonitorMemory(object? obj)
    {
        var lastLogTime = DateTime.MinValue;

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
                    Log.Write("GlobalMemoryStatusEx failed");
                    break;
                }

                if (DateTime.Now > lastLogTime)
                {
                    Log.Write(@$"GlobalMemoryStatusEx:
dwMemoryLoad: {memoryStatus.dwMemoryLoad}
ullAvailPhys: {memoryStatus.ullAvailPhys}
ullTotalPhys: {memoryStatus.ullTotalPhys}
ullAvailVirtual: {memoryStatus.ullAvailVirtual}
ullTotalVirtual: {memoryStatus.ullTotalVirtual}");

                    lastLogTime = DateTime.Now + TimeSpan.FromMinutes(1);
                }

                if (memoryStatus.dwMemoryLoad < MemoryLimitPercentage)
                {
                    continue;
                }

                Log.Important($"WARNING: Low memory {memoryStatus.dwMemoryLoad}", ConsoleColor.Red);

                ForEach(cache => cache.OnMemoryLow());
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

    private static void ForEach(Action<IMemoryCache> action)
    {
        lock (s_caches)
        {
            var cleanup = false;

            foreach (var cache in s_caches)
            {
                if (cache.TryGetTarget(out var target))
                {
                    action(target);
                }
                else
                {
                    cleanup = true;
                }
            }

            if (cleanup)
            {
                s_caches.RemoveAll(item => !item.TryGetTarget(out _));
            }
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307", Justification = "Interop")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Interop")]
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
