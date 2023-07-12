// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.HtmlToPdf;

internal static class RetryHelper
{
    public static void Retry(
        Action action,
        TimeSpan[] retryIntervals)
    {
        Guard.ArgumentNotNull(action, nameof(action));
        Guard.ArgumentNotNull(retryIntervals, nameof(retryIntervals));
        Guard.Argument(
            () => retryIntervals.All(timeout => timeout.CompareTo(TimeSpan.Zero) >= 0),
            nameof(retryIntervals),
            $"{nameof(retryIntervals)} should only contain non-negative timeout value.");

        int retryCount = 0;
        while (true)
        {
            if (retryCount > 0)
            {
                Thread.Sleep(retryIntervals[retryCount - 1]);
            }
            try
            {
                action();
                break;
            }
            catch
            {
                if (retryCount++ < retryIntervals.Length)
                {
                    Console.WriteLine("Convert failed, will retry in " + retryIntervals[retryCount - 1] + "seconds");
                    continue;
                }
                throw;
            }
        }
    }
}
