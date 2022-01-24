// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal static class ParallelUtility
{
    private static readonly int s_maxParallelism = Math.Max(8, Environment.ProcessorCount * 2);
    private static readonly ParallelOptions s_parallelOptions = new() { MaxDegreeOfParallelism = s_maxParallelism };

    public static void ForEach<T>(LogScope scope, ErrorBuilder errors, IEnumerable<T> source, Action<T> action)
    {
        var done = 0;
        var total = source.Count();

        Parallel.ForEach(source, s_parallelOptions, item =>
        {
            try
            {
                action(item);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex);
            }
            catch
            {
                Console.WriteLine($"Error processing '{item}'");
                throw;
            }

            Progress.Update(scope, Interlocked.Increment(ref done), total);
        });
    }
}
