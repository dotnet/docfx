// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.PdfConverter
{
    using System;
    using System.Linq;
    using System.Threading;

    internal static class RetryHelper
    {
        public static void Retry(Action action, TimeSpan[] retryIntervals)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (retryIntervals == null)
            {
                throw new ArgumentNullException(nameof(retryIntervals));
            }

            if (retryIntervals.Any(timeout => timeout.CompareTo(TimeSpan.Zero) < 0))
            {
                throw new ArgumentException($"{nameof(retryIntervals)} should only contain non-negative timeout value.");
            }

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
