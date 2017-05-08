// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;
    using System.Linq;
    using System.Threading;

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
}
