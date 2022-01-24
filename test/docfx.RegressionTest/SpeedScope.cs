// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal static class SpeedScope
{
    public static (string method, int percentage)[] FindHotMethods(string fileName, int top = 30)
    {
        // https://github.com/jlfwong/speedscope/blob/master/src/lib/file-format-spec.ts
        var frame = new { name = "" };
        var @event = new { type = "", frame = 0, at = 0.0 };
        var profile = new { type = "", name = "", unit = "", startValue = 0.0, endValue = 0.0, events = new[] { @event } };
        var spec = new { shared = new { frames = new[] { frame } }, profiles = new[] { profile } };

        var data = JsonConvert.DeserializeAnonymousType(File.ReadAllText(fileName), spec) ?? throw new ArgumentNullException();
        var inclusiveTime = new double[data.shared.frames.Length];
        var totalTime = 0.0;

        Parallel.ForEach(data.profiles, profile =>
        {
            if (profile.type != "evented" || profile.unit != "milliseconds")
            {
                throw new NotSupportedException();
            }

            var stack = new Stack<(int frame, double at)>();
            var localTotalTime = 0.0;
            var localInclusiveTime = new double[data.shared.frames.Length];

            foreach (var e in profile.events)
            {
                switch (e.type)
                {
                    case "O":
                        stack.Push((e.frame, e.at));
                        break;

                    case "C":
                        var (frame, at) = stack.Pop();
                        if (frame != e.frame)
                        {
                            throw new InvalidOperationException($"Stack corruption: {frame} -> {e.frame}");
                        }

                        var time = e.at - at;
                        localTotalTime += time;
                        localInclusiveTime[frame] += time;
                        foreach (var (item, _) in stack)
                        {
                            localInclusiveTime[item] += time;
                        }
                        break;
                }
            }

            lock (inclusiveTime)
            {
                for (var i = 0; i < inclusiveTime.Length; i++)
                {
                    inclusiveTime[i] += localInclusiveTime[i];
                }
                totalTime += localTotalTime;
            }
        });

        return data.shared.frames.Zip(inclusiveTime)
            .Where(item => IsMyCode(item.First.name))
            .OrderByDescending(item => item.Second)
            .Select(item => (item.First.name, (int)Math.Round(100 * item.Second / totalTime)))
            .Take(top)
            .ToArray();

        static bool IsMyCode(string method)
        {
            return method.Contains("!") &&
                  !method.Contains("?!?") &&
                  !method.StartsWith("System.", StringComparison.OrdinalIgnoreCase);
        }
    }
}
